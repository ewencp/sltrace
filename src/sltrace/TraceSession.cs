/*  SLTrace
 *  TraceSession.cs
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
using OpenMetaverse;
using System.Collections.Generic;
using System.Diagnostics;

namespace SLTrace {

class TraceSession {
    public TraceSession(Config cfg) {
        mConfig = cfg;
        mClient = new GridClient();
        mTracers = new List<ITracer>();
        mController = null;

        // We turn as many things off as possible -- features are *opt in* by
        // default, meaning specific loggers must enable these features if they
        // need them
        mClient.Settings.MULTIPLE_SIMS = false;
        mClient.Throttle.Asset = 0;
        mClient.Throttle.Cloud = 0;
        mClient.Throttle.Land = 0;
        mClient.Throttle.Texture = 0;
        mClient.Throttle.Wind = 0;

        // Set up our session management callbacks
        mClient.Network.OnConnected += new NetworkManager.ConnectedCallback(this.ConnectHandler);
        mClient.Network.OnDisconnected += new NetworkManager.DisconnectedCallback(this.DisconnectHandler);
    }

    public GridClient Client {
        get { return mClient; }
    }

    public void AddTracer(ITracer tr) {
        if (tr != null)
            mTracers.Add(tr);
    }

    public IController Controller {
        set {
            mController = value;
        }
    }

    public Config Config {
        get {
            return mConfig;
        }
    }

    public void Run(TimeSpan duration) {
        // Notify all ITracers of start
        foreach(ITracer tr in mTracers)
            tr.StartTrace(this);
        // Notify controller
        if (mController != null) {
            Debug.Assert(mClient.Self != null, "Avatar AgentManager is null");
            mController.StartTrace(this, mClient.Self);
        }

        bool logged_in;
        if (Config.HasStart) {
            string start_loc = NetworkManager.StartLocation(Config.StartSim, Config.StartX, Config.StartY, Config.StartZ);
            logged_in = mClient.Network.Login(
                mConfig.FirstName, mConfig.LastName, mConfig.Password,
                Config.UserAgent, start_loc, Config.UserVersion
            );
        }
        else {
            logged_in = mClient.Network.Login(
                mConfig.FirstName, mConfig.LastName, mConfig.Password,
                Config.UserAgent, Config.UserVersion
            );
        }

        Console.WriteLine("Login message: " + mClient.Network.LoginMessage);

        if (!logged_in)
            return;

        // Sleep for the specified duration, async callbacks do all the real
        // work.
        DateTime start = DateTime.Now;
        while(true) {
            System.Threading.Thread.Sleep(1000);
            if (mController != null)
                mController.Update();
            if (DateTime.Now - start > duration)
                break;
        }

        // And logout...
        mClient.Network.Logout();

        // Notify all ITracers of start
        foreach(ITracer tr in mTracers)
            tr.StopTrace();
    }

    private void ConnectHandler(object sender) {
        Console.WriteLine("I'm connected to the simulator");
    }

    private void DisconnectHandler(NetworkManager.DisconnectType type, string message) {
        Console.WriteLine("Got disconnected from sim: " + message);
    }

    private Config mConfig;
    private GridClient mClient;
    private List<ITracer> mTracers;
    private IController mController;
} // class TraceSession

} // namespace SLTrace