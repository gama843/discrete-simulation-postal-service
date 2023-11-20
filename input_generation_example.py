stop_time = 15
travel_time = 6
world_dim = 32
wait_time = 42

base_wait = 0
base_departure = 0
trucks = []
counter = 0

def generate_trucks_vertical(counter, wait_time, delay, count, start1, stop1, step1, start2, stop2, step2):
    for i in range(count):
        stops = []
        base_departure = 0
        for j in range(start1, stop1, step1):
            stop = {}
            stop['coords'] = (i, j)
            if (j == start1):
                stop['arrival'] = 0
            else:
                stop['arrival'] = base_departure + travel_time
            stop['wait'] = 0 if j != 15 else wait_time
            stop['departure'] = stop_time + stop['wait']
            # base_departure = stop['departure']
            stops.append(stop)
        
        for j in range(start2, stop2, step2):
            stop = {}
            stop['coords'] = (i, j)
            stop['arrival'] = base_departure + travel_time
            stop['wait'] = 0 if j != 15 else wait_time
            stop['departure'] = stop_time + stop['wait']
            # base_departure = stop['departure']
            stops.append(stop)

        truck = {}
        truck['ID'] = counter
        truck['stops'] = stops
        if type(package_load) == list:
            truck['package_load_policy'] = [item for item in package_load]
        else:
            truck['package_load_policy'] = package_load
        if type(package_unload) == list:
            truck['package_unload_policy'] = [item for item in package_unload]
        else:
            truck['package_unload_policy'] = package_unload
        truck['delay'] = delay
        trucks.append(truck)
        counter += 1
    return counter

def generate_trucks_horizontal(counter, wait_time, delay, start1, stop1, step1, start2, stop2, step2):
    stops = []
    base_departure = 0
    for i in range(start1, stop1, step1):
        stop = {}
        stop['coords'] = (i, 15)
        if (i == start1):
            stop['arrival'] = 0
        else:
            stop['arrival'] = base_departure + travel_time
        stop['wait'] = 0 if i != 15 else wait_time
        stop['departure'] = stop_time + stop['wait']
        # base_departure = stop['departure']
        stops.append(stop)

    for i in range(start2, stop2, step2):
        stop = {}
        stop['coords'] = (i, 15)
        if (i == 0 and base_departure == 0):
            stop['arrival'] = 6
        else:
            stop['arrival'] = base_departure + travel_time
        stop['wait'] = 0 if i != 15 else wait_time
        stop['departure'] = stop_time + stop['wait']
        # base_departure = stop['departure']
        stops.append(stop)

    truck = {}
    truck['ID'] = counter
    truck['stops'] = stops
    if type(package_load) == list:
        truck['package_load_policy'] = [item for item in package_load]
    else:
        truck['package_load_policy'] = package_load
    if type(package_unload) == list:
        truck['package_unload_policy'] = [item for item in package_unload]
    else:
        truck['package_unload_policy'] = package_unload
    truck['delay'] = delay
    trucks.append(truck)
    counter += 1

    return counter

#generates trucks driving from the bottom to the middle of the plane
package_load = ['L all 0 14', 'L en_route 15 29', 'L all 30 30']
package_unload = ['U nen_route 15 15']
for i in range(12):
    delay = 100 * i
    counter = generate_trucks_vertical(counter, wait_time, delay, world_dim, 0, 16, 1, 14, -1, -1)

#generates trucks driving from the top to the middle of the plane
package_load = ['L all 0 15', 'L en_route 16 31', 'L all 32 32']
package_unload = ['U nen_route 16 16']
for i in range(12):
    delay = 100 * i
    counter = generate_trucks_vertical(counter, 0, delay, world_dim, 31, 14, -1, 16, 32, 1)

#generates 1 horizontal truck on row 16 travelling from the start to the middle of the plane
package_load = ['L xlc 0 14', 'L xsc 15 29', 'L xlc 30 30']
package_unload = ['U xlc 15 15', 'U xec 0 30']
for i in range(12):
    delay = 100 * i
    counter = generate_trucks_horizontal(counter, wait_time, delay, 0, 16, 1, 14, -1, -1)

#generates 1 horizontal truck on row 16 travelling from the end to the middle of the plane
package_load = ['L xsc 0 15', 'L xlc 16 31', 'L xsc 32 32']
package_unload = ['U xsc 16 16', 'U xec 0 32']
for i in range(12):
    delay = 100 * i
    counter = generate_trucks_horizontal(counter, 0, delay, 31, 14, -1, 16, 32, 1)

with open('test_data.txt', 'w') as f:
    for truck in trucks:
        for stop in truck['stops']:
            print('S', stop['coords'][0], stop['coords'][1], stop['arrival'], stop['wait'], stop['departure'], file=f)
        for item in truck['package_load_policy']:
            print(item, file=f)
        for item in truck['package_unload_policy']:
            print(item, file=f)
        print('D', truck['delay'], file=f)
        print('A', truck['ID'], file=f)
        
print('Test data succesfully generated!')