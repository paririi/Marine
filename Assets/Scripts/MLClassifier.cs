using System;
using UnityEngine;
using Unity.InferenceEngine;

public class EnvironmentMLClassifier : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private TextAsset labelsFile;

    [Header("Input Settings")]
    [SerializeField] private int imageSize = 224;

    private Model runtimeModel;
    private Worker worker;
    private string[] labels;

    private void Awake()
    {
        InitializeModel();
        LoadLabels();
    }

    private void OnDestroy()
    {
        // Release the inference worker when this object is destroyed.
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
            // Fallback labels in case no labels file is assigned.
            labels = new[] { "sand", "sea" };
            return;
        }

        labels = labelsFile.text.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries
        );
    }

    public bool TryPredict(Texture2D sourceTexture, out EnvironmentType predictedEnvironment, out float confidence)
    {
        predictedEnvironment = EnvironmentType.Unknown;
        confidence = 0f;

        if (worker == null || sourceTexture == null)
            return false;

        Texture2D resizedTexture = null;
        Tensor<float> inputTensor = null;
        Tensor<float> outputTensor = null;
        Tensor<float> cpuTensor = null;

        try
        {
            // Resize the source image to the input size expected by the model.
            resizedTexture = ResizeTexture(sourceTexture, imageSize, imageSize);

            // Convert the resized texture into a tensor.
            inputTensor = TextureToTensor(resizedTexture);

            // Run inference.
            worker.Schedule(inputTensor);
            outputTensor = worker.PeekOutput() as Tensor<float>;

            if (outputTensor == null)
                return false;

            // Read model output back to CPU so we can inspect values safely.
            cpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;

            if (cpuTensor == null || cpuTensor.count < 2)
                return false;

            // Find the class with the highest output score.
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

            // Map the winning class index to the label from labels.txt.
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

            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"EnvironmentMLClassifier: Prediction failed. {e.Message}");
            return false;
        }
        finally
        {
            inputTensor?.Dispose();
            outputTensor?.Dispose();
            cpuTensor?.Dispose();

            if (resizedTexture != null)
                Destroy(resizedTexture);
        }
    }

    private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;

        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return result;
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

                // Send raw 0-255 RGB values.
                // MobileNetV2 preprocess input is already inside the exported model,
                // so Unity must NOT normalize to [-1, 1] again.
                data[index++] = pixel.r * 255f;
                data[index++] = pixel.g * 255f;
                data[index++] = pixel.b * 255f;
            }
        }

        TensorShape shape = new TensorShape(1, imageSize, imageSize, 3);
        return new Tensor<float>(shape, data);
    }
}