import csv
import os

class CSVLogger:
    def __init__(self, filename="states.csv"):
        self.filename = filename
        # Open file in write mode (this will overwrite an existing file)
        self.file = open(filename, "w", newline='')
        self.writer = csv.writer(self.file)
        # Write a header row. Adjust the headers to match your state vector.
        self.writer.writerow([
            "step",
            "gold",
            "health",
            "miners",
            "swordsmen",
            "archers",
            "stateValue",
            "nearby_resources_available",
            "enemy_health",
            "enemy_miners",
            "enemy_swordsmen",
            "enemy_archers",
            "enemies_in_vicinity",
            "episode_time",
            "action"
        ])

    def log_state(self, step, state):
        """
        Log the state data along with the step number.
        `state` is expected to be a sequence or numpy array of length 13.
        """
        row = [step] + list(state)
        self.writer.writerow(row)
        self.file.flush()  # Optional: flush after each write to ensure data is written immediately.

    def close(self):
        self.file.close()