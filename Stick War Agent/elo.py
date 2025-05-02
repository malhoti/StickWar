# elo.py
import json
import os
import math

class EloManager:
    def __init__(self, path="elos.json", k=32, enabled=True):
        self.path = path
        self.k = k
        self.enabled = enabled
        # load or initialise
        if os.path.exists(self.path):
            with open(self.path, "r") as f:
                self.ratings = json.load(f)
        else:
            self.ratings = {}  # e.g. {"PPO":1200}
    def get(self, name):
        return self.ratings.get(name, 1200)

    def set(self, name, score):
        self.ratings[name] = score

    def save(self):
        with open(self.path, "w") as f:
            json.dump(self.ratings, f, indent=2)

    def _expected(self, ra, rb):
        return 1.0 / (1 + 10 ** ((rb - ra) / 400))

    def update(self, agent_a, agent_b, result):
       
        if not self.enabled:
            return

        ra = self.get(agent_a)
        rb = self.get(agent_b)

        ea = self._expected(ra, rb)
        eb = self._expected(rb, ra)
        sa = result
        sb = 1.0 - result

        na = ra + self.k * (sa - ea)
        nb = rb + self.k * (sb - eb)

        self.set(agent_a, na)
        self.set(agent_b, nb)
        self.save()
