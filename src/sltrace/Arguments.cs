/*  SLTrace
 *  Arguments.cs
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

namespace SLTrace {

class Arguments {

    /** Tries to split an argument into a key-value pair according to the format
     *  --key=value.
     */
    public static void AsKeyValue(string arg, out string key, out string value) {
        string[] parts = arg.Split(new Char[] {'='}, 2);

        string keyout = parts[0];
        string valueout = parts.Length > 1 ? parts[1] : "";

        keyout = keyout.Trim('-');

        key = keyout;
        value = valueout;
    }

    /** Parses a sequence of arguments into a dictionary of key-value pairs. */
    public static Dictionary<string, string> Parse(IEnumerable<string> args) {
        Dictionary<string, string> arg_dict = new Dictionary<string, string>();
        foreach(string arg in args) {
            string key, value;
            AsKeyValue(arg, out key, out value);
            arg_dict.Add(key, value);
        }
        return arg_dict;
    }

    /** Splits a string containing a sequence of arguments into an array of
     *  individual arguments. Arguments are denoted by the starting characters
     *  " --".
     */
    public static string[] Split(string args_as_string) {
        if (String.IsNullOrEmpty(args_as_string))
            return new string[] {};
        // FIXME this is a bit naive, we really need to also handle quoted args
        // as well
        string[] args = args_as_string.Split(' ');
        return args;
    }

} // class Arguments

} // namespace SLTrace
