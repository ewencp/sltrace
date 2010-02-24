/*  SLTrace
 *  JSON.cs
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
using System.IO;
using System.Diagnostics;

namespace SLTrace {

interface JSONValue {
    String Encoded { get; }
}

class JSONString : JSONValue {
    public JSONString(String orig) {
        mOrig = orig;
    }
    private String mOrig;

    private String Escape() {
        String retval = @"""";

        foreach(char c in mOrig) {
            switch (c) {
              case '\t':
                retval += @"\t";
                break;
              case '\n':
                retval += @"\n";
                break;
              case '\r':
                retval += @"\r";
                break;
              case '\f':
                retval += @"\f";
                break;
              case '\b':
                retval += @"\b";
                break;
              case '<':
                retval += @"\u003c";
                break;
              case '>':
                retval += @"\u003e";
                break;
              case '"':
                retval += "\\\"";
                break;
              case '\'':
                retval += @"\u0027";
                break;
              case '\\':
                retval += @"\\";
                break;
              default:
                retval += c;
                break;
            }
        }
        retval += @"""";
        return retval;
    }

    public String Encoded {
        get { return Escape(); }
    }
}
class JSONBool : JSONValue {
    public JSONBool(bool orig) {
        mOrig = orig;
    }
    private bool mOrig;

    public String Encoded {
        get { if (mOrig) return "true"; else return "false"; }
    }
}
class JSONNull : JSONValue {
    public String Encoded {
        get { return "null"; }
    }
}
class JSONFloat : JSONValue {
    public JSONFloat(double orig) {
        mOrig = orig;
    }
    private double mOrig;

    public String Encoded {
        get { return mOrig.ToString(); }
    }
}
class JSONInt : JSONValue {
    public JSONInt(long orig) {
        mOrig = orig;
    }
    private long mOrig;

    public String Encoded {
        get { return mOrig.ToString(); }
    }
}

/** Utility methods for generating JSON files.  This provides a bunch of tools
 *  for manually building up a JSON file -- without any validation -- because
 *  this is much simpler than pulling in an entire JSON library or building a
 *  real one ourselves.  Mostly we expect simple lists of events, so it'll be
 *  tough to screw things up too much.
 */
class JSON {
    private enum ElementType {
        Object,
        Array
    }

    /** An element record specifies what type of element we're currently
     *  generating and how much progress we've made, i.e. whether an operation
     *  is required to add a separator for the previous element or not.  We'll
     *  maintain a stack of these, and they also allow us to do a small bit of
     *  sanity checking on operations, most especially End*() calls.
     */
    private class ElementRecord {
        public ElementRecord(ElementType type) {
            Type = type;
            PastFirst = false;
        }

        public ElementType Type;
        public bool PastFirst;
    }


    public JSON(TextWriter writer) {
        Debug.Assert(writer != null, "TextWriter provided to SLTrace.JSON is null.");
        mWriter = writer;
        mElementStack = new Stack<ElementRecord>();
    }

    public void Finish() {
        mWriter.Close();
        mWriter = null;
    }

    public void BeginObject() {
        BeginObject(false);
    }

    // is_field indicates whether this call is due to adding an object field, in
    // which case we don't need to check for separators since its already been
    // done when adding the field name
    private void BeginObject(bool is_field) {
        if (!is_field)
            CheckParentSeparator();

        mElementStack.Push( new ElementRecord(ElementType.Object) );
        mWriter.Write("{\n");
    }

    public void EndObject() {
        Debug.Assert(mElementStack.Peek().Type == ElementType.Object);

        if (mElementStack.Peek().PastFirst)
            mWriter.Write("\n");

        mWriter.Write("}\n");
        mElementStack.Pop();

        // If we have a parent, it must be past its first element now
        if (mElementStack.Count > 0)
            mElementStack.Peek().PastFirst = true;
    }

    public void BeginArray() {
        BeginArray(false);
    }

    public void BeginArray(bool is_field) {
        if (!is_field)
            CheckParentSeparator();

        mElementStack.Push( new ElementRecord(ElementType.Array) );
        mWriter.Write("[\n");
    }

    public void EndArray() {
        Debug.Assert(mElementStack.Peek().Type == ElementType.Array);

        if (mElementStack.Peek().PastFirst)
            mWriter.Write("\n");

        mWriter.Write("]\n");
        mElementStack.Pop();

        // If we have a parent, it must be past its first element now
        if (mElementStack.Count > 0)
            mElementStack.Peek().PastFirst = true;
    }


    /** Add a field that is a subobject.  This starts the object definition, so
     *  the user only needs to add fields and then call EndObject().
     */
    public void ObjectField(String key) {
        AddFieldKey(key);
        BeginObject(true);
    }

    /** Add a field that is an array.  This starts the array definition, so
     *  the user only needs to add fields and then call EndArray().
     */
    public void ArrayField(String key) {
        AddFieldKey(key);
        BeginArray(true);
    }

    /** Add a field to the current parent object. */
    public void Field(String key, JSONValue val) {
        Debug.Assert(mElementStack.Peek().Type == ElementType.Object);
        AddFieldKey(key);
        mWriter.Write("{0}", val.Encoded);
        mElementStack.Peek().PastFirst = true;
    }

    public void Value(JSONValue val) {
        Debug.Assert(mElementStack.Peek().Type == ElementType.Array);

        CheckParentSeparator();
        mWriter.Write(val.Encoded);

        mElementStack.Peek().PastFirst = true;
    }

    // Handles adding object field key
    private void AddFieldKey(String key) {
        Debug.Assert(mElementStack.Peek().Type == ElementType.Object);

        CheckParentSeparator();
        mWriter.Write(" {0} : ", (new JSONString(key)).Encoded);
    }


    // Checks if a parent object/array requires a separator to be added
    private void CheckParentSeparator() {
        if (mElementStack.Count > 0 && mElementStack.Peek().PastFirst)
            mWriter.Write(",\n");
    }


    TextWriter mWriter;
    Stack<ElementRecord> mElementStack;
} // class JSON

} // namespace SLTrace