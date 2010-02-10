#!/usr/bin/python

import sys
import vec3

class MotionPath:
    """
    MotionPath is a sequence of positions with timestamps.  The
    iterator is over an ordered list of (time, pos_vec3) tuples.  Note
    that the timestamps are actually deltas w.r.t. a starting time,
    stored in MotionPath.start.
    """

    def __init__(self, start, points=None):
        self.start = start
        self._points = points
        if not self._points:
            self._points = []

    def __iter__(self):
        return self._points.__iter__()

    def __getitem__(self,key):
        return self._points[key]

    def __len__(self):
        return len(self._points)

    def waypoints(self):
        return self._points

    def timestamps(self):
        return [ts for ts,p in self._points]

    def points(self):
        return [p for ts,p in self._points]

    def squeeze(self, fudge=0.0):
        """
        "Squeeze" this motion path by getting rid of duplicate or
        close to duplicate neighboring updates.

        Keyword arguments:
        fudge -- a distance to allow between two points under which
        they will still be considered equal, so small floating point
        errors or tiny movements are discarded
        """

        equals_func = vec3.equals
        if fudge > 0.0: equals_func = vec3.create_delta_equals(fudge)

        last = None
        new_points = []
        for time,point in self._points:
            if equals_func(point, last): continue

            new_points.append( (time,point) )
            last = point

        self._points = new_points
        return self
