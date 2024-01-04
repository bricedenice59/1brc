My Proposal in C# for the One Billion Row Java Challenge > https://github.com/gunnarmorling/1brc

## Running the Challenge

This repository contains two programs:

* `CreateMeasurements`: Creates the file _measurements.txt_ in a given directory with a configurable number of random measurement values
* `CalculateAverage`: Calculates the average values for the file _measurements.txt_

Execute the following steps to run the challenge:

1. Build the console project and run:
    ```
    ./CreateMeasurement.sh 1000000000 output_directory
    ```

    This will take a few minutes.
    **Attention:** the generated file has a size of approx. **14 GB**, so make sure to have enough diskspace
