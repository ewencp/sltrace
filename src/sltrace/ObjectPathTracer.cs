/*  SLTrace
 *  ObjectPathTracer.cs
 *
 *  Copyright (c) 2010, Ewen Cheslack-Postava
 *  All rights reserved.
 *
 *  Redistribution and use in source and binary forms, with or without
 *  modification, are permitted provided that the following conditions are
 *  met:
 *  * Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *  * Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in
 *    the documentation and/or other materials provided with the
 *    distribution.
 *  * Neither the name of SLTrace nor the names of its contributors may
 *    be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
 * IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
 * TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
 * PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER
 * OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenMetaverse.Rendering;

namespace SLTrace {

/** Records events concerning object locations - addition and removal from
 *  interest set, position and velocity updates, size updates, etc.
 */
class ObjectPathTracer : ITracer {
    public void StartTrace(TraceSession parent) {
        mParent = parent;

        string renderer_name = "OpenMetaverse.Rendering.Meshmerizer.dll";
        string renderer_path = System.IO.Path.Combine(parent.Config.BinaryPath, renderer_name);
        mRenderer = OpenMetaverse.Rendering.RenderingLoader.LoadRenderer(renderer_path);

        mObjectsByLocalID = new Dictionary<uint, Primitive>();
        mObjectsByID = new Dictionary<UUID, Primitive>();
        mObjectParents = new Dictionary<UUID, uint>();

        mParent.Client.Objects.OnObjectDataBlockUpdate +=
            new ObjectManager.ObjectDataBlockUpdateCallback(this.ObjectDataBlockUpdateHandler);

        mParent.Client.Objects.OnNewAvatar +=
            new ObjectManager.NewAvatarCallback(this.NewAvatarHandler);

        mParent.Client.Objects.OnNewPrim +=
            new ObjectManager.NewPrimCallback(this.NewPrimHandler);

        mParent.Client.Objects.OnNewAttachment +=
            new ObjectManager.NewAttachmentCallback(this.NewAttachmentHandler);

        // Note: We only do OnObjectTerseUpdate because OnObjectUpdate
        // is redundant and less informative.
        mParent.Client.Objects.OnObjectTerseUpdate +=
            new ObjectManager.ObjectUpdatedTerseCallback(this.ObjectUpdatedTerseHandler);

        mParent.Client.Objects.OnObjectKilled +=
            new ObjectManager.KillObjectCallback(this.ObjectKilledHandler);


        mParent.Client.Objects.OnObjectProperties +=
            new ObjectManager.ObjectPropertiesCallback(this.ObjectPropertiesHandler);

        mParent.Client.Self.Movement.Camera.Far = 512.0f;


        // We output JSON, one giant list of events
        System.IO.TextWriter streamWriter =
            new System.IO.StreamWriter("object_paths.txt");
        mJSON = new JSON(streamWriter);
        mJSON.BeginArray();

        mStartTime = DateTime.Now;
        mJSON.BeginObject();
        JSONStringField("event", "started");
        mJSON.Field("time", new JSONString( mStartTime.ToString() ));
        mJSON.EndObject();
    }

    public void StopTrace() {
        mJSON.EndArray();
        mJSON.Finish();
    }

    private void ComputeBounds(Primitive prim) {
        SimpleMesh mesh = mRenderer.GenerateSimpleMesh(prim, DetailLevel.High);

        if (mesh.Vertices.Count == 0)
            return;

        Vector3 vert_min = mesh.Vertices[0].Position;
        Vector3 vert_max = mesh.Vertices[0].Position;

        foreach(Vertex v in mesh.Vertices) {
            vert_min = Vector3.Min(vert_min, v.Position);
            vert_max = Vector3.Max(vert_max, v.Position);
        }

        lock(mJSON) {
            mJSON.BeginObject();
            JSONStringField("event", "size");
            JSONUUIDField("id", prim.ID);
            JSONTimeSpanField("time", SinceStart);
            JSONVector3Field("min", vert_min);
            JSONVector3Field("max", vert_max);
            JSONVector3Field("scale", prim.Scale);
            mJSON.EndObject();
        }
    }

    private void CheckMembershipWithLocation(Simulator sim, String primtype, Primitive prim) {
        CheckMembership(sim, primtype, prim, true);
    }

    private void CheckMembership(Simulator sim, String primtype, Primitive prim) {
        CheckMembership(sim, primtype, prim, false);
    }

    private void CheckMembership(Simulator sim, String primtype, Primitive prim, bool with_loc) {
        if (prim.ID == UUID.Zero)
            return;

        lock(this) {
            if (mObjectsByLocalID.ContainsKey(prim.LocalID)) {
                if (mObjectsByLocalID[prim.LocalID] != prim) {
                    Console.WriteLine("Object addition for existing local id: " + prim.LocalID.ToString());
                    return;
                }
            }

            bool is_update = false;
            if (mObjectsByID.ContainsKey(prim.ID)) {
                if (mObjectsByID[prim.ID] != prim)
                    Console.WriteLine("Conflicting global ID for different prim instances: {0}.", prim.ID.ToString());
                is_update = true;
            }

            mObjectsByLocalID[prim.LocalID] = prim;
            mObjectsByID[prim.ID] = prim;

            Primitive parentPrim = null;
            bool known_parent = mObjectsByLocalID.ContainsKey(prim.ParentID);
            if (known_parent)
                parentPrim = mObjectsByLocalID[prim.ParentID];

            // Either this is brand new, or its an update and we should only
            // store the object if an important feature has changed. Currently
            // the only important feature we track is ParentID.
            if (!is_update ||
                mObjectParents[prim.ID] != prim.ParentID)
                StoreNewObject(primtype, prim, prim.ParentID, parentPrim);

            mObjectParents[prim.ID] = prim.ParentID;


            if (is_update) // Everything else should only happen for brand new objects
                return;

            RequestObjectProperties(sim, prim);
            if (!known_parent && prim.ParentID != 0)
                RequestObjectProperties(sim, prim.ParentID);

            ComputeBounds(prim);
            if (with_loc)
                StoreLocationUpdate(prim);
        }
    }

    private void RemoveMembership(uint localid) {
        UUID fullid = UUID.Zero;
        lock(this) {
            if (mObjectsByLocalID.ContainsKey(localid)) {
                Primitive prim = mObjectsByLocalID[localid];
                mObjectsByLocalID.Remove(localid);
                mObjectsByID.Remove(prim.ID);
                mObjectParents.Remove(prim.ID);
                fullid = prim.ID;
            }
        }

        if (fullid == UUID.Zero) return;
        lock(mJSON) {
            mJSON.BeginObject();
            JSONStringField("event", "kill");
            JSONTimeSpanField("time", SinceStart);
            JSONUUIDField("id", fullid);
            mJSON.EndObject();
        }
    }

    // Note: Because we need to use this both for new prims/avatars and for
    // ObjectUpdates, and ObjectUpdates don't update the primitive until *after*
    // the callback, we need to pass the information in explicitly.
    private void StoreLocationUpdate(Primitive prim, Vector3 pos, Vector3 vel, Quaternion rot, Vector3 angvel) {
        lock(mJSON) {
            mJSON.BeginObject();
            JSONStringField("event", "loc");
            JSONUUIDField("id", prim.ID);
            JSONTimeSpanField("time", SinceStart);
            JSONVector3Field("pos", pos);
            JSONVector3Field("vel", vel);
            JSONQuaternionField("rot", rot);
            JSONVector3Field("angvel", angvel);
            mJSON.EndObject();
        }
    }

    private void StoreLocationUpdate(Primitive prim) {
        StoreLocationUpdate(prim, prim.Position, prim.Velocity, prim.Rotation, prim.AngularVelocity);
    }
    private void StoreLocationUpdate(Primitive prim, ObjectUpdate update) {
        StoreLocationUpdate(prim, update.Position, update.Velocity, update.Rotation, update.AngularVelocity);
    }


    private void RequestObjectProperties(Simulator sim, Primitive prim) {
        mParent.Client.Objects.SelectObject(sim, prim.LocalID);
    }
    private void RequestObjectProperties(Simulator sim, uint primid) {
        mParent.Client.Objects.SelectObject(sim, primid);
    }

    private void ObjectDataBlockUpdateHandler(Simulator simulator, Primitive prim, Primitive.ConstructionData constructionData, ObjectUpdatePacket.ObjectDataBlock block, ObjectUpdate update, NameValue[] nameValues) {
        // Note: We can't do membership or really much useful here since the
        // primitive doesn't have its information filled in quite yet.  Instead,
        // we're forced to use the OnNew* events, which get called *after* all
        // the data has been filled in.
        //Console.WriteLine("Object: " + update.LocalID.ToString() + " - " + update.Position.ToString() + " " + update.Velocity.ToString());
    }

    private void StoreNewObject(String type, Primitive obj, uint parentLocal, Primitive parent) {
        lock(mJSON) {
            mJSON.BeginObject();
            JSONStringField("event", "add");
            JSONTimeSpanField("time", SinceStart);
            JSONStringField("type", type);
            JSONUInt32Field("local", obj.LocalID);
            JSONUUIDField("id", obj.ID);
            if (parentLocal != 0)
                JSONUInt32Field("parent_local", parentLocal);
            if (parent != null)
                JSONUUIDField("parent", parent.ID);
            mJSON.EndObject();
        }

        // Selecting avatars doesn't work for getting object properties, but the
        // same properties are available immediately here.
        Avatar avtr = obj as Avatar;
        if (avtr != null)
            StoreObjectProperties(avtr.ID, avtr.Name, "(Avatar: N/A)");
    }

    private void StoreObjectProperties(UUID id, string name, String description) {
        lock(mJSON) {
            mJSON.BeginObject();
            JSONStringField("event", "properties");
            JSONUUIDField("id", id);
            JSONStringField("name", name);
            JSONStringField("description", description);
            mJSON.EndObject();
        }
    }


    private void NewAvatarHandler(Simulator simulator, Avatar avatar, ulong regionHandle, ushort timeDilation) {
        CheckMembershipWithLocation(simulator, "avatar", avatar);
    }
    private void NewPrimHandler(Simulator simulator, Primitive prim, ulong regionHandle, ushort timeDilation) {
        CheckMembershipWithLocation(simulator, "prim", prim);
    }
    private void NewAttachmentHandler(Simulator simulator, Primitive prim, ulong regionHandle, ushort timeDilation) {
        CheckMembershipWithLocation(simulator, "attachment", prim);
    }

    private void ObjectUpdatedTerseHandler(Simulator simulator, Primitive prim, ObjectUpdate update, ulong regionHandle, ushort timeDilation) {
        CheckMembership(simulator, "terse", prim);
        StoreLocationUpdate(prim);
    }

    private void ObjectKilledHandler(Simulator simulator, uint objectID) {
        RemoveMembership(objectID);
    }

    private void ObjectPropertiesHandler(Simulator simulator, Primitive.ObjectProperties properties) {
        StoreObjectProperties(properties.ObjectID, properties.Name, properties.Description);
    }



    // JSON Encoding helpers
    private void JSONStringField(String name, String val) {
        mJSON.Field(name, new JSONString(val));
    }
    private void JSONUInt32Field(String name, uint val) {
        mJSON.Field(name, new JSONInt((long)val));
    }
    private void JSONUUIDField(String name, UUID val) {
        mJSON.Field(name, new JSONString( val.ToString() ));
    }
    private void JSONVector3Field(String name, Vector3 vec) {
        mJSON.ObjectField(name);
        mJSON.Field("x", new JSONString(vec.X.ToString()));
        mJSON.Field("y", new JSONString(vec.Y.ToString()));
        mJSON.Field("z", new JSONString(vec.Z.ToString()));
        mJSON.EndObject();
    }
    private void JSONQuaternionField(String name, Quaternion vec) {
        mJSON.ObjectField(name);
        mJSON.Field("w", new JSONString(vec.W.ToString()));
        mJSON.Field("x", new JSONString(vec.X.ToString()));
        mJSON.Field("y", new JSONString(vec.Y.ToString()));
        mJSON.Field("z", new JSONString(vec.Z.ToString()));
        mJSON.EndObject();
    }
    private void JSONTimeSpanField(String name, TimeSpan val) {
        mJSON.Field(name, new JSONString( val.TotalMilliseconds.ToString() + "ms" ));
    }

    // Time helpers
    private TimeSpan SinceStart {
        get { return DateTime.Now - mStartTime; }
    }

    private IEnumerable<Primitive> RootObjects {
        get { return mObjectsByLocalID.Values.Where(obj => obj.ParentID == 0); }
    }

    private TraceSession mParent;
    IRendering mRenderer;
    private DateTime mStartTime;
    private Dictionary<uint, Primitive> mObjectsByLocalID; // All tracked
                                                           // objects, by LocalID
    private Dictionary<UUID, Primitive> mObjectsByID; // All tracked objects, by
                                                      // global UUID
    private Dictionary<UUID, uint> mObjectParents; // LocalID of parents of all
                                                   // tracked objects

    private JSON mJSON; // Stores JSON formatted output event stream
} // class RawPacketTracer

} // namespace SLTrace