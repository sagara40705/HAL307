using System;
using System.IO;
using System.Text;
using UnityEngine;

public class PythonCompatMfccFeatureExtractor : MonoBehaviour
{
    public const int MfccCount = 24;
    public const int TargetWidth = 64;

    private const int SampleRate = 16000;
    private const int TargetSampleCount = 16000;
    private const int FftSize = 256;
    private const int HopLength = 256;
    private const int MelFilterCount = 128;
    private const float Amin = 1e-10f;
    private const float TopDb = 80f;

    [Header("Debug")]
    [SerializeField] private bool logFeatureStats = true;
    [SerializeField] private bool exportCsvOnNextExtraction = false;
    [SerializeField] private string csvFileName = "python_compat_mfcc.csv";

    private float[] hannWindow;
    private float[,] melFilterBank;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        hannWindow = CreateHannWindow(FftSize);
        melFilterBank = CreateSlaneyMelFilterBank(MelFilterCount, FftSize, SampleRate);
        initialized = true;
    }

    public float[,] ExtractMfcc(float[] samples)
    {
        Initialize();

        float[] fixedSamples = PadOrTrim(samples, TargetSampleCount);
        float[] centeredSamples = PadCenter(fixedSamples, FftSize / 2);

        int frameCount = 1 + Mathf.Max(0, (centeredSamples.Length - FftSize) / HopLength);
        float[,] melPower = new float[MelFilterCount, frameCount];

        for (int frame = 0; frame < frameCount; frame++)
        {
            int start = frame * HopLength;
            float[] frameBuffer = new float[FftSize];

            for (int i = 0; i < FftSize; i++)
            {
                frameBuffer[i] = centeredSamples[start + i] * hannWindow[i];
            }

            float[] powerSpectrum = ComputePowerSpectrum(frameBuffer);

            for (int m = 0; m < MelFilterCount; m++)
            {
                float sum = 0f;
                for (int b = 0; b < powerSpectrum.Length; b++)
                {
                    sum += melFilterBank[m, b] * powerSpectrum[b];
                }

                melPower[m, frame] = sum;
            }
        }

        PowerToDbInPlace(melPower);

        float[,] mfcc = ComputeDctOrtho(melPower, MfccCount);
        float[,] fixedMfcc = PadOrTrimFrames(mfcc, TargetWidth);

        NormalizeGlobal(fixedMfcc);
        LogFeatureStatsIfEnabled(samples, frameCount, fixedMfcc);
        ExportCsvIfRequested(fixedMfcc);

        return fixedMfcc;
    }

    private float[] PadOrTrim(float[] samples, int targetLength)
    {
        float[] output = new float[targetLength];
        if (samples == null || samples.Length == 0)
        {
            return output;
        }

        int copyLength = Mathf.Min(samples.Length, targetLength);
        Array.Copy(samples, output, copyLength);
        return output;
    }

    private float[] PadCenter(float[] samples, int pad)
    {
        float[] output = new float[samples.Length + pad * 2];
        Array.Copy(samples, 0, output, pad, samples.Length);
        return output;
    }

    private float[] ComputePowerSpectrum(float[] frame)
    {
        int bins = FftSize / 2 + 1;
        float[] spectrum = new float[bins];
        float[] realValues = new float[FftSize];
        float[] imagValues = new float[FftSize];

        Array.Copy(frame, realValues, FftSize);
        ForwardFft(realValues, imagValues);

        for (int k = 0; k < bins; k++)
        {
            float real = realValues[k];
            float imag = imagValues[k];
            spectrum[k] = real * real + imag * imag;
        }

        return spectrum;
    }

    private void ForwardFft(float[] real, float[] imag)
    {
        int n = real.Length;

        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
            {
                j ^= bit;
            }

            j ^= bit;

            if (i < j)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imag[i], imag[j]) = (imag[j], imag[i]);
            }
        }

        for (int length = 2; length <= n; length <<= 1)
        {
            float angle = -2f * Mathf.PI / length;
            float wLengthReal = Mathf.Cos(angle);
            float wLengthImag = Mathf.Sin(angle);

            for (int i = 0; i < n; i += length)
            {
                float wReal = 1f;
                float wImag = 0f;
                int halfLength = length >> 1;

                for (int j = 0; j < halfLength; j++)
                {
                    int evenIndex = i + j;
                    int oddIndex = evenIndex + halfLength;

                    float oddReal = real[oddIndex] * wReal - imag[oddIndex] * wImag;
                    float oddImag = real[oddIndex] * wImag + imag[oddIndex] * wReal;

                    float evenReal = real[evenIndex];
                    float evenImag = imag[evenIndex];

                    real[evenIndex] = evenReal + oddReal;
                    imag[evenIndex] = evenImag + oddImag;
                    real[oddIndex] = evenReal - oddReal;
                    imag[oddIndex] = evenImag - oddImag;

                    float nextWReal = wReal * wLengthReal - wImag * wLengthImag;
                    wImag = wReal * wLengthImag + wImag * wLengthReal;
                    wReal = nextWReal;
                }
            }
        }
    }

    private void PowerToDbInPlace(float[,] melPower)
    {
        int rows = melPower.GetLength(0);
        int cols = melPower.GetLength(1);
        float maxDb = float.NegativeInfinity;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float value = 10f * Mathf.Log10(Mathf.Max(Amin, melPower[r, c]));
                melPower[r, c] = value;

                if (value > maxDb)
                {
                    maxDb = value;
                }
            }
        }

        float floorDb = maxDb - TopDb;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (melPower[r, c] < floorDb)
                {
                    melPower[r, c] = floorDb;
                }
            }
        }
    }

    private float[,] ComputeDctOrtho(float[,] melDb, int coeffCount)
    {
        int melCount = melDb.GetLength(0);
        int frames = melDb.GetLength(1);
        float[,] mfcc = new float[coeffCount, frames];

        for (int c = 0; c < coeffCount; c++)
        {
            float scale = c == 0 ? Mathf.Sqrt(1f / melCount) : Mathf.Sqrt(2f / melCount);

            for (int t = 0; t < frames; t++)
            {
                float sum = 0f;
                for (int m = 0; m < melCount; m++)
                {
                    sum += melDb[m, t] * Mathf.Cos(Mathf.PI * c * (m + 0.5f) / melCount);
                }

                mfcc[c, t] = sum * scale;
            }
        }

        return mfcc;
    }

    private float[,] PadOrTrimFrames(float[,] mfcc, int targetWidth)
    {
        int rows = mfcc.GetLength(0);
        int sourceWidth = mfcc.GetLength(1);
        float[,] output = new float[rows, targetWidth];
        int copyWidth = Mathf.Min(sourceWidth, targetWidth);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < copyWidth; c++)
            {
                output[r, c] = mfcc[r, c];
            }
        }

        return output;
    }

    private void NormalizeGlobal(float[,] data)
    {
        int rows = data.GetLength(0);
        int cols = data.GetLength(1);
        int count = rows * cols;
        double sum = 0.0;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                sum += data[r, c];
            }
        }

        float mean = (float)(sum / count);
        double varianceSum = 0.0;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float d = data[r, c] - mean;
                varianceSum += d * d;
            }
        }

        float std = Mathf.Sqrt((float)(varianceSum / count)) + 1e-6f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                data[r, c] = (data[r, c] - mean) / std;
            }
        }
    }

    private float[] CreateHannWindow(int size)
    {
        float[] window = new float[size];
        for (int i = 0; i < size; i++)
        {
            window[i] = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * i / size);
        }

        return window;
    }

    private float[,] CreateSlaneyMelFilterBank(int filterCount, int fftSizeValue, int sr)
    {
        int bins = fftSizeValue / 2 + 1;
        float[,] filters = new float[filterCount, bins];
        float[] fftFrequencies = new float[bins];

        for (int i = 0; i < bins; i++)
        {
            fftFrequencies[i] = (float)i * sr / fftSizeValue;
        }

        float minMel = HzToMelSlaney(0f);
        float maxMel = HzToMelSlaney(sr / 2f);
        float[] melFrequencies = new float[filterCount + 2];

        for (int i = 0; i < melFrequencies.Length; i++)
        {
            float mel = Mathf.Lerp(minMel, maxMel, (float)i / (melFrequencies.Length - 1));
            melFrequencies[i] = MelToHzSlaney(mel);
        }

        for (int m = 0; m < filterCount; m++)
        {
            float lower = melFrequencies[m];
            float center = melFrequencies[m + 1];
            float upper = melFrequencies[m + 2];
            float enorm = 2f / Mathf.Max(1e-12f, upper - lower);

            for (int b = 0; b < bins; b++)
            {
                float freq = fftFrequencies[b];
                float lowerSlope = (freq - lower) / Mathf.Max(1e-12f, center - lower);
                float upperSlope = (upper - freq) / Mathf.Max(1e-12f, upper - center);
                filters[m, b] = Mathf.Max(0f, Mathf.Min(lowerSlope, upperSlope)) * enorm;
            }
        }

        return filters;
    }

    private float HzToMelSlaney(float hz)
    {
        const float fSp = 200f / 3f;
        const float minLogHz = 1000f;
        const float minLogMel = minLogHz / fSp;
        float logStep = Mathf.Log(6.4f) / 27f;

        if (hz < minLogHz)
        {
            return hz / fSp;
        }

        return minLogMel + Mathf.Log(hz / minLogHz) / logStep;
    }

    private float MelToHzSlaney(float mel)
    {
        const float fSp = 200f / 3f;
        const float minLogHz = 1000f;
        const float minLogMel = minLogHz / fSp;
        float logStep = Mathf.Log(6.4f) / 27f;

        if (mel < minLogMel)
        {
            return mel * fSp;
        }

        return minLogHz * Mathf.Exp(logStep * (mel - minLogMel));
    }

    private void LogFeatureStatsIfEnabled(float[] originalSamples, int frameCount, float[,] mfcc)
    {
        if (!logFeatureStats)
        {
            return;
        }

        int rows = mfcc.GetLength(0);
        int cols = mfcc.GetLength(1);
        int count = rows * cols;
        float min = mfcc[0, 0];
        float max = mfcc[0, 0];
        double sum = 0.0;
        bool hasInvalid = false;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float value = mfcc[r, c];
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    hasInvalid = true;
                }

                if (value < min)
                {
                    min = value;
                }

                if (value > max)
                {
                    max = value;
                }

                sum += value;
            }
        }

        float mean = (float)(sum / count);
        double varianceSum = 0.0;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float d = mfcc[r, c] - mean;
                varianceSum += d * d;
            }
        }

        float std = Mathf.Sqrt((float)(varianceSum / count));
        int inputCount = originalSamples == null ? 0 : originalSamples.Length;

        Debug.Log($"[PythonCompatMFCC] inputSamples={inputCount}, framesBeforePadTrim={frameCount}, finalShape={rows}x{cols}, min={min:F6}, max={max:F6}, mean={mean:F6}, std={std:F6}, hasNaNOrInfinity={hasInvalid}");
    }

    private void ExportCsvIfRequested(float[,] mfcc)
    {
        if (!exportCsvOnNextExtraction)
        {
            return;
        }

        exportCsvOnNextExtraction = false;

        string path = Path.Combine(Application.persistentDataPath, string.IsNullOrWhiteSpace(csvFileName) ? "python_compat_mfcc.csv" : csvFileName);
        StringBuilder sb = new StringBuilder();
        int rows = mfcc.GetLength(0);
        int cols = mfcc.GetLength(1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (c > 0)
                {
                    sb.Append(',');
                }

                sb.Append(mfcc[r, c].ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            }

            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[PythonCompatMFCC] CSV exported: {path}");
    }
}
