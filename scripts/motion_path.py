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

    def __len__(self):
        return len(self._points)

    def waypoints(self):
        return self._points

    def timestamps(self):
        return [ts for ts,p in self._points]

    def points(self):
        return [p for ts,p in self._points]

    def squeeze(self):
        """
        "Squeeze" this motion path by getting rid of duplicate or
        close to duplicate neighboring updates.
        """

        last = None
        new_points = []
        for time,point in self._points:
            if vec3.equals(point, last): continue

            new_points.append( (time,point) )
            last = point

        self._points = new_points
        return self
