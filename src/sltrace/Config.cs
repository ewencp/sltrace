/*  SLTrace
 *  Config.cs
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
using System.Reflection;
using System.Configuration;
using System.Collections.Generic;
using System.Diagnostics;

namespace SLTrace {

/** Config represents the configuration for a single trace session, in terms of
 *  a single client login instance (as opposed to a trace system which may
 *  include traces via a larger number of client connections).
 */
class Config {
    public Config(string binpath, Dictionary<string, string> arg_map) {
        mBinPath = binpath;

        mFirstName = "";
        mLastName = "";
        mPassword = "";

        if (arg_map.ContainsKey("first"))
            mFirstName = arg_map["first"];
        if (arg_map.ContainsKey("last"))
            mLastName = arg_map["last"];
        if (arg_map.ContainsKey("password"))
            mPassword = arg_map["password"];

        mStartURL = null;

        if (arg_map.ContainsKey("url")) {
            string[] parts = arg_map["url"].Split(new Char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            Debug.Assert(parts.Length == 5);
            Debug.Assert(parts[0] == "secondlife");
            mStartURL = new string[] { parts[1], parts[2], parts[3], parts[4] };
        }
    }

    // The path the binary sltrace.exe is being run from, useful for finding
    // sister binaries and plugins.
    public string BinaryPath {
        get { return mBinPath; }
    }

    public string FirstName {
        get { return mFirstName; }
    }

    public string LastName {
        get { return mLastName; }
    }

    public string Password {
        get { return mPassword; }
    }

    public static string UserAgent {
        get {
            var attr = GetAssemblyAttribute<AssemblyVersionAttribute>();
            if (attr != null)
                return attr.Version;
            return string.Empty;
        }
    }

    public static string UserVersion {
        get {
            var attr = GetAssemblyAttribute<AssemblyVersionAttribute>();
            if (attr != null)
                return attr.Version;
            return string.Empty;
        }
    }

    public bool HasStart {
        get { return mStartURL != null; }
    }

    public string StartSim {
        get { return mStartURL[0]; }
    }

    public int StartX {
        get { return Int32.Parse(mStartURL[1]); }
    }

    public int StartY {
        get { return Int32.Parse(mStartURL[2]); }
    }

    public int StartZ {
        get { return Int32.Parse(mStartURL[3]); }
    }


    // Helper method gets attributes of current assembly
    private static T GetAssemblyAttribute<T>() where T : Attribute {
        object[] attributes = Assembly.GetExecutingAssembly()
            .GetCustomAttributes(typeof(T), true);
        if (attributes == null || attributes.Length == 0) return null;
        return (T)attributes[0];
    }

    private string mBinPath;
    private string mFirstName;
    private string mLastName;
    private string mPassword;
    private string[] mStartURL;
} // class Config

} // namespace SLTrace