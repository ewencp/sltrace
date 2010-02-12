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

    def start_time(self):
        first_update_t,first_update_pos = self._points[0]
        return first_update_t

    def end_time(self):
        last_update_t,last_update_pos = self._points[-1]
        return last_update_t

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

    def interpolate(self, t):
        """
        Interpolates the position of the object on this path at the
        specified time.  Times outside the range of updates are always
        clamped to the first or last location update.
        """

        # Standard bounds checks
        first_update_t,first_update_pos = self._points[0]
        if t <= first_update_t: return first_update_pos

        last_update_t,last_update_pos = self._points[-1]
        if t >= last_update_t: return last_update_pos

        # Otherwise, find the right pair of updates
        prev_t,prev_pos = self._points[0]
        for cur_t,cur_pos in self._points:
            if t >= prev_t and t < cur_t:
                alpha = float(t - prev_t) / float(cur_t - prev_t)
                return vec3.add(vec3.scale(cur_pos, alpha), vec3.scale(prev_pos, (1.0 - alpha)))

            prev_t,prev_pos = cur_t,cur_pos
