
using UnityEngine;

public class MfccFeatureExtractor : MonoBehaviour
{
    public const int MfccCount = 20;
    public const int TargetWidth = 32;

    [Header("Audio Settings")]
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int fftSize = 512;
    [SerializeField] private int hopLength = 256;
    [SerializeField] private int melFilterCount = 40;
    [SerializeField] private float preEmphasis = 0.97f;

    [Header("Normalization")]
    [SerializeField] private bool normalizePerFeature = true;

    private float[] hammingWindow;
    private float[,] melFilterBank;
    private bool initialized = false;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized) return;

        hammingWindow = CreateHammingWindow(fftSize);
        melFilterBank = CreateMelFilterBank(melFilterCount, fftSize, sampleRate);

        initialized = true;
    }

    public float[,] ExtractMfcc(float[] samples)
    {
        Initialize();

        if (samples == null || samples.Length == 0)
        {
            return new float[MfccCount, TargetWidth];
        }

        float[] processed = PreEmphasis(samples, preEmphasis);

        int frameCount = 1 + Mathf.Max(0, (processed.Length - fftSize) / hopLength);
        if (processed.Length < fftSize)
        {
            frameCount = 1;
        }

        float[,] melSpectrogram = new float[melFilterCount, frameCount];

        for (int frame = 0; frame < frameCount; frame++)
        {
            int start = frame * hopLength;

            float[] frameBuffer = new float[fftSize];
            for (int i = 0; i < fftSize; i++)
            {
                int idx = start + i;
                float s = (idx < processed.Length) ? processed[idx] : 0f;
                frameBuffer[i] = s * hammingWindow[i];
            }

            float[] powerSpectrum = ComputePowerSpectrum(frameBuffer);
            float[] melEnergies = ApplyMelFilterBank(powerSpectrum);

            for (int m = 0; m < melFilterCount; m++)
            {
                // log(0) 回避
                melSpectrogram[m, frame] = Mathf.Log(melEnergies[m] + 1e-6f);
            }
        }

        float[,] mfcc = ComputeDct(melSpectrogram, MfccCount);

        if (normalizePerFeature)
        {
            NormalizeEachRow(mfcc);
        }

        return ResizeToTargetWidth(mfcc, TargetWidth);
    }

    // 互換用
    public float[,] ExtractDummyMfcc(float[] samples)
    {
        return ExtractMfcc(samples);
    }

    private float[] PreEmphasis(float[] input, float coeff)
    {
        float[] output = new float[input.Length];
        if (input.Length == 0) return output;

        output[0] = input[0];
        for (int i = 1; i < input.Length; i++)
        {
            output[i] = input[i] - coeff * input[i - 1];
        }

        return output;
    }

    private float[] ComputePowerSpectrum(float[] frame)
    {
        int bins = fftSize / 2 + 1;
        float[] spectrum = new float[bins];

        for (int k = 0; k < bins; k++)
        {
            float real = 0f;
            float imag = 0f;

            for (int n = 0; n < fftSize; n++)
            {
                float angle = 2f * Mathf.PI * k * n / fftSize;
                real += frame[n] * Mathf.Cos(angle);
                imag -= frame[n] * Mathf.Sin(angle);
            }

            spectrum[k] = real * real + imag * imag;
        }

        return spectrum;
    }

    private float[] ApplyMelFilterBank(float[] powerSpectrum)
    {
        int bins = powerSpectrum.Length;
        float[] melEnergies = new float[melFilterCount];

        for (int m = 0; m < melFilterCount; m++)
        {
            float sum = 0f;
            for (int b = 0; b < bins; b++)
            {
                sum += melFilterBank[m, b] * powerSpectrum[b];
            }
            melEnergies[m] = sum;
        }

        return melEnergies;
    }

    private float[,] ComputeDct(float[,] melLogSpectrogram, int coeffCount)
    {
        int melCount = melLogSpectrogram.GetLength(0);
        int frames = melLogSpectrogram.GetLength(1);

        float[,] mfcc = new float[coeffCount, frames];

        for (int c = 0; c < coeffCount; c++)
        {
            for (int t = 0; t < frames; t++)
            {
                float sum = 0f;
                for (int m = 0; m < melCount; m++)
                {
                    sum += melLogSpectrogram[m, t] *
                           Mathf.Cos(Mathf.PI * c * (m + 0.5f) / melCount);
                }

                mfcc[c, t] = sum;
            }
        }

        return mfcc;
    }

    private void NormalizeEachRow(float[,] data)
    {
        int rows = data.GetLength(0);
        int cols = data.GetLength(1);

        for (int r = 0; r < rows; r++)
        {
            float mean = 0f;
            for (int c = 0; c < cols; c++)
            {
                mean += data[r, c];
            }
            mean /= cols;

            float variance = 0f;
            for (int c = 0; c < cols; c++)
            {
                float d = data[r, c] - mean;
                variance += d * d;
            }
            variance /= cols;

            float std = Mathf.Sqrt(variance + 1e-6f);

            for (int c = 0; c < cols; c++)
            {
                data[r, c] = (data[r, c] - mean) / std;
            }
        }
    }

    private float[,] ResizeToTargetWidth(float[,] src, int targetWidth)
    {
        int rows = src.GetLength(0);
        int srcWidth = src.GetLength(1);

        float[,] dst = new float[rows, targetWidth];

        if (srcWidth == targetWidth)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < targetWidth; c++)
                {
                    dst[r, c] = src[r, c];
                }
            }
            return dst;
        }

        if (srcWidth <= 1)
        {
            for (int r = 0; r < rows; r++)
            {
                float v = src[r, 0];
                for (int c = 0; c < targetWidth; c++)
                {
                    dst[r, c] = v;
                }
            }
            return dst;
        }

        for (int r = 0; r < rows; r++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                float srcPos = (float)x * (srcWidth - 1) / (targetWidth - 1);
                int left = Mathf.FloorToInt(srcPos);
                int right = Mathf.Min(left + 1, srcWidth - 1);
                float t = srcPos - left;

                dst[r, x] = Mathf.Lerp(src[r, left], src[r, right], t);
            }
        }

        return dst;
    }

    private float[] CreateHammingWindow(int size)
    {
        float[] window = new float[size];

        for (int n = 0; n < size; n++)
        {
            window[n] = 0.54f - 0.46f * Mathf.Cos(2f * Mathf.PI * n / (size - 1));
        }

        return window;
    }

    private float[,] CreateMelFilterBank(int filterCount, int fftSizeValue, int sr)
    {
        int bins = fftSizeValue / 2 + 1;
        float[,] filters = new float[filterCount, bins];

        float fMin = 0f;
        float fMax = sr / 2f;

        float melMin = HzToMel(fMin);
        float melMax = HzToMel(fMax);

        float[] melPoints = new float[filterCount + 2];
        for (int i = 0; i < melPoints.Length; i++)
        {
            melPoints[i] = Mathf.Lerp(melMin, melMax, (float)i / (melPoints.Length - 1));
        }

        float[] hzPoints = new float[melPoints.Length];
        int[] binPoints = new int[melPoints.Length];

        for (int i = 0; i < melPoints.Length; i++)
        {
            hzPoints[i] = MelToHz(melPoints[i]);
            binPoints[i] = Mathf.FloorToInt((fftSizeValue + 1) * hzPoints[i] / sr);
            binPoints[i] = Mathf.Clamp(binPoints[i], 0, bins - 1);
        }

        for (int m = 1; m <= filterCount; m++)
        {
            int left = binPoints[m - 1];
            int center = binPoints[m];
            int right = binPoints[m + 1];

            if (center <= left) center = left + 1;
            if (right <= center) right = center + 1;
            right = Mathf.Min(right, bins - 1);

            for (int k = left; k < center; k++)
            {
                filters[m - 1, k] = (float)(k - left) / Mathf.Max(1, center - left);
            }

            for (int k = center; k < right; k++)
            {
                filters[m - 1, k] = (float)(right - k) / Mathf.Max(1, right - center);
            }
        }

        return filters;
    }

    private float HzToMel(float hz)
    {
        return 2595f * Mathf.Log10(1f + hz / 700f);
    }

    private float MelToHz(float mel)
    {
        return 700f * (Mathf.Pow(10f, mel / 2595f) - 1f);
    }
}
