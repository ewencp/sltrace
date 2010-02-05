#!/usr/bin/python

import sys
try:
    import simplejson as json
except:
    import json
from uuid import UUID

def parse_time(val):
    """
    Parses a time since start from a string in a trace.  Returns the delta
    as a floating point # of seconds.
    """
    assert(val[-2:] == 'ms')
    return (float(val[:-2]) / 1000.0)

class ObjectPathTrace:
    """
    ObjectPathTrace provides a convenient interface to object path trace data
    generated by sltrace.  It can load from a file or accept a filtered version
    of a trace.  It is mostly useful as a base for operations on path trace
    data since it mostly provides utility methods including ways to filter the
    raw trace data, extracting certain types of events, events associated with
    specific objects, and methods for cleaning up the raw data (e.g. attempting
    to fill in missing parent information).
    """

    def __init__(self, trace_file=None, raw=None, start_time=None):
        """
        Create a new ObjectPathTrace. Only one source of data should be
        specified.

        Keyword arguments:
        trace_file -- name of JSON trace file
        raw -- raw Python representation of JSON, i.e. an array of
               events (default None)
        start_time -- start time to use for this trace. Overrides any start
                      time specified in the raw trace. (default None)
        """

        # Get raw data
        if raw: self._orig = raw
        elif trace_file: self._orig = json.load(open(trace_file))
        else: self._orig = []
        # Filter and set start time from data. If specified, override with
        # user start time
        started_evts = [x for x in self._orig if x['event'] == 'started']
        if len(started_evts) > 0:
            self._start_time = started_evts[0]['time'] # FIXME convert to datetime
        if start_time: self._start_time = start_time

        # Cached data:
        # 1) By objects, object categories
        self._objects = None  # List of objects
        self._avatars = None  # List of avatars
        # 2) By event type
        self._additions = None # List of addition events
        self._removals = None # List of removal events

        self._filled_parents = False

    def objects(self):
        """Returns a list of object UUIDs encountered in this trace."""
        if not self._objects:
            id_set = {}
            for x in self.addition_events():
                if 'id' in x: id_set[UUID(x['id'])] = 1
            self._objects = id_set.keys()

        return self._objects

    def avatars(self):
        """Returns a list of avatar UUIDs encountered in this trace."""
        if not self._avatars:
            id_set = {}
            for x in self.addition_events():
                if (x['type'] == 'avatar' and
                    'id' in x):
                    id_set[UUID(x['id'])] = 1
            self._avatars = id_set.keys()

        return self._avatars

    def object(self, objid):
        """
        Returns a new ObjectPathTrace containing a subset of the original
        events, containing only events pertaining to the specified object.
        """
        object_subset = [x for x in self._orig
                         if 'id' in x and UUID(x['id']) == objid]
        return ObjectPathTrace(raw=object_subset, start_time=self._start_time)


    def events(self):
        """Returns a list of all the events in this trace."""
        return self._orig

    def __iter__(self):
        return self._orig.__iter__()

    def addition_events(self):
        """Returns a list of addition events in this trace."""
        if not self._additions:
            self._additions = [x for x in self._orig if x['event'] == 'add']
        return self._additions

    def removal_events(self):
        """Returns a list of kill events in this trace."""
        if not self._additions:
            self._additions = [x for x in self._orig if x['event'] == 'kill']
        return self._additions


    def fill_parents(self, report=False):
        """
        Attempts to fill in the 'parent' field of addition events with the
        appropriate UUID, based on the 'parent_local' field.  This is necessary
        because sometimes the parent object's local ID hasn't been registered
        when an addition event occurs.
        """
        if self._filled_parents: return

        unfilled_parent_events = [x for x in self.addition_events()
                                  if 'parent' not in x and
                                  'parent_local' in x]

        # Bucket by local parent id in order to do lookups
        unfilled_by_parentid = {}
        for x in unfilled_parent_events:
            if x['parent_local'] not in unfilled_by_parentid:
                unfilled_by_parentid[x['parent_local']] = []
            unfilled_by_parentid[x['parent_local']].append(x)

        # For each parent id, try to find the nearest and fill in the events
        no_options = 0
        for parentid,events in unfilled_by_parentid.items():
            candidate_parents = [x for x in self.addition_events()
                                 if x['local'] == parentid]

            for evt in events:
                added_time = parse_time(evt['time'])
                if len(candidate_parents) == 0:
                    no_options += 1
                    continue
                best_candidate = min(candidate_parents, key=lambda x:(parse_time(x['time'])-added_time))
        if no_options > 0 and report:
            print no_options, 'objects found with local parent ID but no matching object.'

        self._filled_parents = True

    def roots(self, ambiguous=False):
        """
        Returns a list of object IDs which are root objects, i.e. they do not
        have a parent ID listed in the trace.

        Keyword arguments:
        ambiguous -- if an object has multiple addition events and they have
                     conflicting child status (i.e. at one time it has a parent
                     ID, at another it doesn't), this controls whether it is
                     reported or not. (Default: False, i.e. they will not be
                     reported)
        """

        obj_info = {} # obj -> (had_parent_bool, had_empty_parent_bool)
        for addition in self.addition_events():
            add_id = UUID(addition['id'])

            # Make sure we have a record of the object
            if add_id not in obj_info:
                obj_info[add_id] = (False, False)

            # Check if we need to mark as not having a parent or add parent to list
            if 'parent_local' in addition:
                obj_info[add_id] = (True, obj_info[add_id][1])
            else:
                obj_info[add_id] = (obj_info[add_id][0], True)

        rootobjs = [objid for (objid,parentinfo) in obj_info.items()
                    if parentinfo[1] and
                    ((not parentinfo[0]) or (parentinfo[0] and ambiguous))]

        return rootobjs

    def parents(self):
        """
        Returns a dict mapping object UUID -> parent UUID. For root objects the
        parent UUID is None. The first parent found is always reported, i.e.
        this method will not handle changes in object ownership.
        """
        self.fill_parents()

        parent_dict = {}
        # Just scan through additions, adding new found relationships
        for addition in self.addition_events():
            obj_id = UUID(addition['id'])
            if obj_id in parent_dict: continue

            par_id = None
            if ('parent' in addition):
                par_id = UUID(addition['parent'])
            parent_dict[obj_id] = par_id

        return parent_dict

def main():
    if len(sys.argv) < 2:
        print "Specify a file."
        return -1

    trace = ObjectPathTrace(sys.argv[1])
    trace.fill_parents(report=True)

    print "Trace file:", sys.argv[1]
    print "Number of objects:", len(trace.objects())
    print "Number of avatars:", len(trace.avatars())
    print "Number of root objects:", len(trace.roots())

    return 0

if __name__ == "__main__":
    sys.exit(main())
