using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace CalculateAverage;

    public unsafe class MeasurementApp : IDisposable
    {
        private const char Separator = ';';
        private const char NewLine = '\n';
        private readonly int _chunkCount = Environment.ProcessorCount;
        
        private readonly MemoryMappedFile _mappedFile;
        private readonly MemoryMappedViewAccessor _viewAccessor;
        private readonly SafeMemoryMappedViewHandle _viewHandle;
        private readonly byte* _pointer;
        private readonly long _fileLength;
        private readonly long _chunkSize;

        //Try a different solution with MemoryMappedFiles https://www.linotes.net/cs/csa/CSA_MemoryMappedFiles/
        public MeasurementApp(string filePath)
        {
            var fileSize = GetFileSize(filePath);
            
            _mappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);
            
            byte* ptr = null;
            _viewAccessor = _mappedFile.CreateViewAccessor(0, fileSize, MemoryMappedFileAccess.Read);
            _viewHandle = _viewAccessor.SafeMemoryMappedViewHandle;
            _viewHandle.AcquirePointer(ref ptr);

            _pointer = ptr;
            _fileLength = fileSize;
            _chunkSize = fileSize / _chunkCount;
        }

        private long GetFileSize(string filePath)
        {
            var fi = new FileInfo(filePath);
            return fi.Length;
        }

        private IEnumerable<(long startIndex, int length)> GetChunks()
        {
            List<(long start, int length)> chunks = [];

            long currentPosition = 0;

            for (var i = 0; i < _chunkCount; i++)
            {
                if (currentPosition + _chunkSize >= _fileLength)
                {
                    chunks.Add((currentPosition, (int)(_fileLength - currentPosition)));
                    break;
                }

                var newPosition = currentPosition + _chunkSize;
                var sp = new ReadOnlySpan<byte>(_pointer + newPosition, (int)_chunkSize);
                var indexNewLine = sp.IndexOf((byte)NewLine);
                newPosition += indexNewLine + 1;

                chunks.Add((currentPosition, (int)(newPosition - currentPosition)));
                currentPosition = newPosition;
            }

            return chunks;
        }

        private Dictionary<string, MeasurementAggregator> ProcessChunk(long startIndex, int length)
        {
            Dictionary<string, MeasurementAggregator> measurementsDic = [];
            var currentPosition = 0;

            while (currentPosition < length)
            {
                var ptr = _pointer + startIndex + currentPosition;

                var span = new ReadOnlySpan<byte>(ptr, length);
                
                var indexSeparator = span.IndexOf((byte)Separator);

                if(indexSeparator == -1)
                    Debugger.Break();

                var stationName = new string((sbyte*)ptr, 0, indexSeparator, Encoding.UTF8);

                indexSeparator += 1;
                span = span.Slice(indexSeparator);

                var indexNewLine = span.IndexOf((byte)NewLine);

                var value = double.Parse(span.Slice(0, indexNewLine));
                
                ref var measurement = ref CollectionsMarshal
                    .GetValueRefOrAddDefault(measurementsDic, stationName, out var exists);

                if (!exists)
                {
                    measurement = new MeasurementAggregator(value);
                }
                else
                {
                    measurement.Combine(new MeasurementAggregator(value));
                }
                
                currentPosition += indexSeparator + indexNewLine + 1;
            }

            return measurementsDic;
        }

        public Dictionary<string, MeasurementAggregator> Process()
        {
            Dictionary<string, MeasurementAggregator> aggregatedMeasurementsDic = [];
            List<Dictionary<string, MeasurementAggregator>> dicChunks = [];
            var processLines = Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(GetChunks(),
                    new ParallelOptions { MaxDegreeOfParallelism = _chunkCount },
                    x =>
                    {
                        dicChunks.Add(ProcessChunk(x.startIndex, x.length));
                    });
                
                aggregatedMeasurementsDic = dicChunks.Aggregate((result, chunk) =>
                {
                    foreach (KeyValuePair<string, MeasurementAggregator> kvp in chunk)
                    {
                        if (!result.TryGetValue(kvp.Key, out _))
                            result.Add(kvp.Key, kvp.Value);
                        else
                        {
                            result[kvp.Key] = result[kvp.Key].Combine(kvp.Value);
                        }
                    }
                    return result;
                });
            });
            processLines.Wait();

            return aggregatedMeasurementsDic;
        }

        public void Dispose()
        {
            _viewHandle.ReleasePointer();
            _viewHandle.Dispose();
            _viewAccessor.Dispose();
            _mappedFile.Dispose();
        }
    }