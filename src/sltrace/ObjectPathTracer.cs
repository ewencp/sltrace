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
        mSeenObjects = new HashSet<UUID>();
        mSeenAvatars = new HashSet<UUID>();

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

        mParent.Client.Self.Movement.Camera.Far = 512.0f;


        // We output JSON, one giant list of events
        System.IO.TextWriter streamWriter =
            new System.IO.StreamWriter("object_paths.txt");
        mJSON = new JSON(streamWriter);
        mJSON.BeginArray();
    }

    public void StopTrace() {
        Console.WriteLine("Saw " + mSeenObjects.Count.ToString() + " Objects.");
        Console.WriteLine("Saw " + mSeenAvatars.Count.ToString() + " Avatars.");

        mJSON.EndArray();
        mJSON.Finish();
    }

    private void CheckMembership(String primtype, Primitive prim) {
        bool do_report = true;
        lock(mActiveObjects) {
            if (mActiveObjects.ContainsKey(prim.LocalID)) {
                if (mActiveObjects[prim.LocalID] != prim.ID)
                    Console.WriteLine("Object addition for existing local id: " + prim.LocalID.ToString());
                do_report = false;
            }
            else {
                mActiveObjects[prim.LocalID] = prim.ID;
                do_report = true;
            }
        }
        if (do_report)
            StoreNewObject(primtype, prim.ID);

        lock(mSeenObjects) {
            if (!mSeenObjects.Contains(prim.ID))
                mSeenObjects.Add(prim.ID);
        }

        lock(mSeenAvatars) {
            if (prim is Avatar && !mSeenAvatars.Contains(prim.ID))
                mSeenAvatars.Add(prim.ID);
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
            mJSON.Field("event", new JSONString("kill"));
            mJSON.Field("id", new JSONString(fullid.ToString()));
            mJSON.EndObject();
        }
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
            mJSON.Field("event", new JSONString("add"));
            mJSON.Field("type", new JSONString(type));
            mJSON.Field("id", new JSONString(id.ToString()));
            mJSON.EndObject();
        }
    }

    private void NewAvatarHandler(Simulator simulator, Avatar avatar, ulong regionHandle, ushort timeDilation) {
        CheckMembership("avatar", avatar);
    }
    private void NewPrimHandler(Simulator simulator, Primitive prim, ulong regionHandle, ushort timeDilation) {
        CheckMembership("prim", prim);
    }
    private void NewAttachmentHandler(Simulator simulator, Primitive prim, ulong regionHandle, ushort timeDilation) {
        CheckMembership("attachment", prim);
    }

    private void ObjectUpdatedTerseHandler(Simulator simulator, Primitive prim, ObjectUpdate update, ulong regionHandle, ushort timeDilation) {
        CheckMembership("terse", prim);
    }

    private void ObjectKilledHandler(Simulator simulator, uint objectID) {
        RemoveMembership(objectID);
    }

    private TraceSession mParent;

    private Dictionary<uint, UUID> mActiveObjects;

    private HashSet<UUID> mSeenObjects;
    private HashSet<UUID> mSeenAvatars;

    private JSON mJSON; // Stores JSON formatted output event stream
} // class RawPacketTracer

} // namespace SLTrace