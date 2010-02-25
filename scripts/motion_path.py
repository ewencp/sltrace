#!/usr/bin/python

import sys
import vec3
import bisect

class MotionPath:
    """
    MotionPath is a sequence of positions with timestamps.  The
    iterator is over an ordered list of (time, pos_vec3) tuples.  Note
    that the timestamps are actually deltas w.r.t. a starting time,
    stored in MotionPath.start.
    """

    def __init__(self, start, points=None):
        self.start = start
        self._timestamps, self._points = zip(*points)

    def __getitem__(self,key):
        return (self._timestamps[key], self._points[key])

    def __len__(self):
        return len(self._points)

    def waypoints(self):
        return zip(self._timestamps, self._points)

    def waypoints_iter(self):
        for idx in range(len(self)):
            yield self[idx]

    def timestamps(self):
        return self._timestamps

    def points(self):
        return self._points

    def start_time(self):
        return self._timestamps[0]

    def end_time(self):
        return self._timestamps[-1]

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
        new_timestamps = []
        new_points = []
        for time,point in self.waypoints_iter():
            if equals_func(point, last): continue

            new_timestamps.append(time)
            new_points.append(point)
            last = point

        self._timestamps = new_timestamps
        self._points = new_points
        return self

    def interpolate(self, t):
        """
        Interpolates the position of the object on this path at the
        specified time.  Times outside the range of updates are always
        clamped to the first or last location update.
        """

        # Standard bounds checks
        first_update_t,first_update_pos = self[0]
        if t <= first_update_t: return first_update_pos

        last_update_t,last_update_pos = self[-1]
        if t >= last_update_t: return last_update_pos

        # Otherwise, find the right pair of updates

        # Start the search on the first element where t >= timestamp to satisfy t >= prev_t
        start_idx = bisect.bisect_left(self.timestamps(), t) - 1
        assert start_idx >= 0

        prev_t,prev_pos = self[start_idx]
        for idx in range(start_idx+1,len(self)):
            cur_t,cur_pos = self[idx]
            if t >= prev_t and t < cur_t:
                alpha = float(t - prev_t) / float(cur_t - prev_t)
                return vec3.add(vec3.scale(cur_pos, alpha), vec3.scale(prev_pos, (1.0 - alpha)))

            prev_t,prev_pos = cur_t,cur_pos

        print t, self.timestamps(), start_idx
