import matplotlib.pyplot as plt
import numpy as np
import sys

MIN_TIME = 2.0
MAX_TIME = 50.0
GT_FILE_PATH = ".\mmvr-hips-gt.txt"

def read_times_and_angles(path):
    times = []
    angles = []
    with open(path, 'r') as f:
        data_str = f.read().split('\n')        
        for i in range(len(data_str) // 2):
            time = float(data_str[2 * i])
            if time > MIN_TIME and time < MAX_TIME:
                times.append(time)
                angles.append(float(data_str[2 * i + 1]))
    return np.array(times), np.array(angles)

gt_times, gt_angles = read_times_and_angles(GT_FILE_PATH)
    
file_paths = sys.argv[1:]
for file_path in file_paths:
    times, angles = read_times_and_angles(file_path)
    # Correct by gt error
    angles -= np.interp(times, gt_times, gt_angles)
        
    print('average:', np.average(angles))
    plt.plot(times, angles)
    
plt.show()
