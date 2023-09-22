import matplotlib.pyplot as plt
import numpy as np
import sys

MIN_TIME = 2.0
MAX_TIME = 50.0

file_paths = sys.argv[1:]

for file_path in file_paths:
    with open(file_path, 'r') as f:
        data_str = f.read()
        data_str = data_str.split('\n')
        times = []
        angles = []
        for i in range(len(data_str) // 2):
            time = float(data_str[2 * i])
            if time > MIN_TIME and time < MAX_TIME:
                times.append(float(data_str[2 * i]))
                angles.append(float(data_str[2 * i + 1]))
    print('average:', np.average(angles))
    plt.plot(times, angles)

plt.xlabel("time (s)", fontsize=15)
plt.ylabel("angular error (degrees)", fontsize=15)
plt.show()
