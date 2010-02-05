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

        # Try to fill in parent IDs for ad
        self.__fill_parents()

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


    def __fill_parents(self):
        """
        Attempts to fill in the 'parent' field of addition events with the
        appropriate UUID, based on the 'parent_local' field.  This is necessary
        because sometimes the parent object's local ID hasn't been registered
        when an addition event occurs.
        """
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
        if no_options > 0:
            print no_options, 'objects found with local parent ID but no matching object.'

def main():
    if len(sys.argv) < 2:
        print "Specify a file."
        return -1

    trace = ObjectPathTrace(sys.argv[1])

    print "Trace file:", sys.argv[1]
    print "Number of objects:", len(trace.objects())
    print "Number of avatars:", len(trace.avatars())

    return 0

if __name__ == "__main__":
    sys.exit(main())
