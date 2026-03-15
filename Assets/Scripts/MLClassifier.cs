using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.InferenceEngine;

public class EnvironmentMLClassifier : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private TextAsset labelsFile;

    [Header("AR Camera")]
    [SerializeField] private ARCameraManager arCameraManager;

    [Header("Input Settings")]
    [SerializeField] private int imageSize = 224;

    private Model runtimeModel;
    private Worker worker;
    private string[] labels;

    private void Awake()
    {
        InitializeModel();
        LoadLabels();

        if (arCameraManager == null)
            arCameraManager = FindFirstObjectByType<ARCameraManager>();
    }

    private void OnDestroy()
    {
        worker?.Dispose();
    }

    private void InitializeModel()
    {
        if (modelAsset == null)
        {
            Debug.LogError("EnvironmentMLClassifier: No model asset assigned.");
            return;
        }

        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.CPU);
    }

    private void LoadLabels()
    {
        if (labelsFile == null)
        {
            labels = new[] { "other", "sand", "sea" };
            return;
        }

        labels = labelsFile.text.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries
        );
    }

    public bool TryPredictFromCamera(out EnvironmentType predictedEnvironment, out float confidence)
    {
        predictedEnvironment = EnvironmentType.Unknown;
        confidence = 0f;

        if (worker == null)
        {
            Debug.LogError("EnvironmentMLClassifier: Worker is not initialized.");
            return false;
        }

        if (arCameraManager == null)
        {
            Debug.LogError("EnvironmentMLClassifier: ARCameraManager is missing.");
            return false;
        }

        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
        {
            Debug.LogWarning("EnvironmentMLClassifier: Could not acquire latest CPU camera image.");
            return false;
        }

        Texture2D cameraTexture = null;
        Tensor<float> inputTensor = null;
        Tensor<float> outputTensor = null;
        Tensor<float> cpuTensor = null;

        try
        {
            cameraTexture = ConvertCpuImageToTexture(cpuImage);
            if (cameraTexture == null)
                return false;

            inputTensor = TextureToTensor(cameraTexture);

            worker.Schedule(inputTensor);
            outputTensor = worker.PeekOutput() as Tensor<float>;

            if (outputTensor == null)
                return false;

            cpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;

            if (cpuTensor == null || cpuTensor.count < 3)
                return false;

            int bestIndex = 0;
            float bestScore = cpuTensor[0];

            for (int i = 1; i < cpuTensor.count; i++)
            {
                if (cpuTensor[i] > bestScore)
                {
                    bestScore = cpuTensor[i];
                    bestIndex = i;
                }
            }

            string predictedLabel = (labels != null && bestIndex < labels.Length)
                ? labels[bestIndex].Trim().ToLower()
                : string.Empty;

            confidence = bestScore;

            if (predictedLabel == "sand")
            {
                predictedEnvironment = EnvironmentType.Sand;
                return true;
            }

            if (predictedLabel == "sea" || predictedLabel == "water")
            {
                predictedEnvironment = EnvironmentType.Water;
                return true;
            }

            if (predictedLabel == "other" || predictedLabel == "unknown")
            {
                predictedEnvironment = EnvironmentType.Unknown;
                return true;
            }

            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"EnvironmentMLClassifier: Prediction failed. {e.Message}");
            return false;
        }
        finally
        {
            cpuImage.Dispose();
            inputTensor?.Dispose();
            outputTensor?.Dispose();
            cpuTensor?.Dispose();

            if (cameraTexture != null)
                Destroy(cameraTexture);
        }
    }

    private Texture2D ConvertCpuImageToTexture(XRCpuImage cpuImage)
    {
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
            outputDimensions = new Vector2Int(imageSize, imageSize),
            outputFormat = TextureFormat.RGB24,
            transformation = XRCpuImage.Transformation.None
        };

        int size = cpuImage.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        try
        {
            cpuImage.Convert(conversionParams, buffer);

            Texture2D texture = new Texture2D(
                imageSize,
                imageSize,
                TextureFormat.RGB24,
                false
            );

            texture.LoadRawTextureData(buffer);
            texture.Apply();

            return texture;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    private Tensor<float> TextureToTensor(Texture2D texture)
    {
        float[] data = new float[imageSize * imageSize * 3];
        int index = 0;

        for (int y = 0; y < imageSize; y++)
        {
            for (int x = 0; x < imageSize; x++)
            {
                Color pixel = texture.GetPixel(x, y);

                // Raw 0-255 RGB values.
                // MobileNetV2 preprocessing is already inside the exported model.
                data[index++] = pixel.r * 255f;
                data[index++] = pixel.g * 255f;
                data[index++] = pixel.b * 255f;
            }
        }

        TensorShape shape = new TensorShape(1, imageSize, imageSize, 3);
        return new Tensor<float>(shape, data);
    }
}