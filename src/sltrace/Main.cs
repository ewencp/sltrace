/*  SLTrace
 *  Main.cs
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

namespace SLTrace {

/** SLTrace is the main trace class -- it load configs, sets up the client
 *  connection, starts and stops logging.
 */
class SLTrace {
    public static GridClient Client = new GridClient();

    private static Config config;

    static void Main(string[] args) {
        config = new Config();

        Client.Network.OnConnected += new NetworkManager.ConnectedCallback(Network_OnConnected);

        var logged_in = Client.Network.Login(config.FirstName, config.LastName, config.Password, Config.UserAgent, Config.UserVersion);

        if (!logged_in) {
            Console.WriteLine("I couldn't log in, here is why: " + Client.Network.LoginMessage);
            Console.WriteLine("press enter to close...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("I logged into Second Life!");
    }

    static void Network_OnConnected(object sender) {
        Console.WriteLine("I'm connected to the simulator, going to greet everyone around me");
        Client.Self.Chat("Hello World!", 0, ChatType.Normal);
        Console.WriteLine("Now I am going to logout of SL.. Goodbye!");
        Client.Network.Logout();
        Console.WriteLine("I am Loged out please press enter to close...");
        Console.ReadLine();
    }
} // class SLTrace
} // namespace SLTrace
