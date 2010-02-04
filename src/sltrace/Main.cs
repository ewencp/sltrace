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
using System.IO;

namespace SLTrace {

/** SLTrace is the main trace class -- it load configs, sets up the client
 *  connection, starts and stops logging.
 */
class SLTrace {
    static void Main(string[] args) {
        // Try to extract information about where the binary is so we can
        // specify in the Config where to search for other binaries
        string[] clargs = Environment.GetCommandLineArgs();
        if (clargs.Length == 0 || String.IsNullOrEmpty(clargs[0])) {
            Console.WriteLine("Invalid program name in command line arguments.");
            Environment.Exit(-1);
            return;
        }
        string progname = Path.GetFullPath(clargs[0]);
        if (!File.Exists(progname)) {
            Console.WriteLine("Couldn't find full path to binary.");
            Environment.Exit(-1);
            return;
        }
        string progdir = Path.GetDirectoryName(progname);

        Config config = new Config(progdir);
        TraceSession session = new TraceSession(config);

        //session.AddTracer(new RawPacketTracer());
        session.AddTracer(new ObjectPathTracer());

        session.Controller = new StaticRotatingController(System.TimeSpan.FromSeconds(30));

        session.Run();
    }
} // class SLTrace
} // namespace SLTrace
