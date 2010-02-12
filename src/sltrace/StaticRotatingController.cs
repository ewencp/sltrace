/*  SLTrace
 *  StaticRotatingController.cs
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
using System.Diagnostics;
using OpenMetaverse;

namespace SLTrace {

/** StaticRotatingController just sits in one location (static) and rotates at
 *  an approximate specified rate, surveying the entire region surrounding it.
 */
class StaticRotatingController : IController {
    public StaticRotatingController(string args) {
        mGridClient = null;
        mAgentManager = null;
        mPeriod = TimeSpan.FromSeconds(30);

        mLastUpdate = DateTime.Now;
        mAngle = 0.0f;
    }

    public void StartTrace(TraceSession parent, OpenMetaverse.AgentManager avatarManager) {
        mGridClient = parent.Client;
        mAgentManager = avatarManager;

        mLastUpdate = DateTime.Now;
    }

    public void Update() {
        Debug.Assert(mGridClient.Settings.SEND_AGENT_UPDATES, "Agent updates are disabled, won't be able to move avatar.");

        DateTime curt = DateTime.Now;
        double rotations = (curt - mLastUpdate).TotalSeconds / mPeriod.TotalSeconds;
        mAngle += (float)(rotations * 360.0);
        while(mAngle >= 360.0f)
            mAngle -= 360.0f;

        Vector3 direction = new Vector3((float)Math.Cos(mAngle), (float)Math.Sin(mAngle), 0.0f);

        Quaternion rot = Vector3.RotationBetween(Vector3.UnitX, Vector3.Normalize(direction));

        mAgentManager.Movement.BodyRotation = rot;
        mAgentManager.Movement.HeadRotation = rot;
        mAgentManager.Movement.Camera.LookDirection(direction);

        mAgentManager.Movement.SendUpdate();

        mLastUpdate = curt;
    }

    private GridClient mGridClient;
    private AgentManager mAgentManager;
    private TimeSpan mPeriod;

    private DateTime mLastUpdate;
    private float mAngle;
} // interface StaticRotatingController

} // namespace SLTrace