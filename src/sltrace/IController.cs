/*  SLTrace
 *  IController.cs
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

namespace SLTrace {

/** Interface for Controller elements, which register with TraceSession and
 *  control the movement of the avatar running the trace.  This effectively
 *  controls what region of the world is recorded.  The IController uses a
 *  polling approach because it is expected that navigation may a) be unrelated
 *  to what is seen in the world (i.e. going for coverage, not responding to
 *  in-world events), and b) updates are relatively infrequent because we are
 *  only observing or, if moving, we are using libomv's auto-pilot.
 */
interface IController {
    /** Invoked once when the parent TraceSession is setup, but before the
     *  connections start.  IControllers should generally only need the
     *  AgentManager to control the avatar, but a reference to the
     *  TraceSession, which can be used to gain full sim access, is also
     *  provided.
     */
    void StartTrace(TraceSession parent, OpenMetaverse.AgentManager avatarManager);

    /** Invoked periodically to allow this controller to update its navigation
     *  plan, possibly generating updates for the sim.
     */
    void Update();
} // interface IController

} // namespace SLTrace