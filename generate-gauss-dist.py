import numpy as np

'''
simple normal distribution
'''

sd = 1000
center = 10000

n_records = 100000000
n_dataslice = 1000
n_group = 2

file_name = 'sd'+str(sd)+'-ds'+str(n_dataslice)+'-gb'+str(n_group)+'-rcd'+str(n_records)+'.csv'
output_path = 'C:\\Users\\izhma\\Data\\artificial-data-for-sqlserver\\' + file_name

f = open(output_path, 'w')

data = np.random.normal(center, sd, (n_dataslice, n_group))
recordID = 1
n_separate = n_group * n_dataslice
for _ in range(0, int(n_records/n_separate)):
    for g in range(1, n_group+1):
        for ds in range(1, n_dataslice+1):
            val = data[ds-1][g-1] / n_separate

            f.write(str(recordID)+', '+str(ds)+', '+str(g)+', '+str(round(val, 2))+'\n')
            recordID += 1

f.close()
