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
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace SLTrace {

/** Records events concerning object locations - addition and removal from
 *  interest set, position and velocity updates, size updates, etc.
 */
class ObjectPathTracer : ITracer {
    public void StartTrace(TraceSession parent) {
        mParent = parent;
        mActiveObjects = new Dictionary<uint,UUID>();

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

    private void CheckMembership(Simulator sim, String primtype, Primitive prim) {
        lock(mActiveObjects) {
            if (mActiveObjects.ContainsKey(prim.LocalID)) {
                if (mActiveObjects[prim.LocalID] != prim.ID)
                    Console.WriteLine("Object addition for existing local id: " + prim.LocalID.ToString());
            }
            else {
                mActiveObjects[prim.LocalID] = prim.ID;
                StoreNewObject(primtype, prim.ID);
                RequestObjectProperties(sim, prim);
            }
        }
    }

    private void RemoveMembership(uint localid) {
        UUID fullid = UUID.Zero;
        lock(mActiveObjects) {
            if (mActiveObjects.ContainsKey(localid)) {
                fullid = mActiveObjects[localid];
                mActiveObjects.Remove(localid);
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

    private void RequestObjectProperties(Simulator sim, Primitive prim) {
        mParent.Client.Objects.SelectObject(sim, prim.LocalID);
    }

    private void ObjectDataBlockUpdateHandler(Simulator simulator, Primitive prim, Primitive.ConstructionData constructionData, ObjectUpdatePacket.ObjectDataBlock block, ObjectUpdate update, NameValue[] nameValues) {
        // Note: We can't do membership or really much useful here since the
        // primitive doesn't have its information filled in quite yet.  Instead,
        // we're forced to use the OnNew* events, which get called *after* all
        // the data has been filled in.
        //Console.WriteLine("Object: " + update.LocalID.ToString() + " - " + update.Position.ToString() + " " + update.Velocity.ToString());
    }

    private void StoreNewObject(String type, UUID id) {
        lock(mJSON) {
            mJSON.BeginObject();
            JSONStringField("event", "add");
            JSONTimeSpanField("time", SinceStart);
            JSONStringField("type", type);
            JSONUUIDField("id", id);
            mJSON.EndObject();
        }
    }

    private void NewAvatarHandler(Simulator simulator, Avatar avatar, ulong regionHandle, ushort timeDilation) {
        CheckMembership(simulator, "avatar", avatar);
    }
    private void NewPrimHandler(Simulator simulator, Primitive prim, ulong regionHandle, ushort timeDilation) {
        CheckMembership(simulator, "prim", prim);
    }
    private void NewAttachmentHandler(Simulator simulator, Primitive prim, ulong regionHandle, ushort timeDilation) {
        CheckMembership(simulator, "attachment", prim);
    }

    private void ObjectUpdatedTerseHandler(Simulator simulator, Primitive prim, ObjectUpdate update, ulong regionHandle, ushort timeDilation) {
        CheckMembership(simulator, "terse", prim);

        // Position update
        lock(mJSON) {
            mJSON.BeginObject();
            JSONStringField("event", "update");
            JSONUUIDField("id", prim.ID);
            JSONTimeSpanField("time", SinceStart);
            JSONVector3Field("pos", update.Position);
            JSONVector3Field("vel", update.Velocity);
            JSONQuaternionField("rot", update.Rotation);
            JSONVector3Field("angvel", update.AngularVelocity);
            mJSON.EndObject();
        }
    }

    private void ObjectKilledHandler(Simulator simulator, uint objectID) {
        RemoveMembership(objectID);
    }

    void ObjectPropertiesHandler(Simulator simulator, Primitive.ObjectProperties properties) {
        lock(mJSON) {
            mJSON.BeginObject();
            JSONStringField("event", "properties");
            JSONUUIDField("id", properties.ObjectID);
            JSONStringField("name", properties.Name);
            JSONStringField("description", properties.Description);
            mJSON.EndObject();
        }
    }



    // JSON Encoding helpers
    private void JSONStringField(String name, String val) {
        mJSON.Field(name, new JSONString(val));
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


    private TraceSession mParent;
    private DateTime mStartTime;

    private Dictionary<uint, UUID> mActiveObjects;

    private JSON mJSON; // Stores JSON formatted output event stream
} // class RawPacketTracer

} // namespace SLTrace