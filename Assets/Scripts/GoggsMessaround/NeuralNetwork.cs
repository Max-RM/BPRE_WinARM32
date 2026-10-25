using System.Collections;
using static System.Math;
using System.Collections.Generic;
using UnityEngine;

public class NeuralNetwork
{
    private int inputSize;
    private int hiddenSize;
    private int outputSize;
    private double[, ] weightsInputHidden;
    private double[, ] weightsHiddenOutput;
    private double[, ] hiddenLayer;
    private double[, ] outputLayer;
    private System.Random random;
    public NeuralNetwork(int inputSize, int hiddenSize, int outputSize)
    {
        this.inputSize = inputSize;
        this.hiddenSize = hiddenSize;
        this.outputSize = outputSize;
        weightsInputHidden = new double[inputSize, hiddenSize];
        weightsHiddenOutput = new double[hiddenSize, outputSize];
        hiddenLayer = new double[1, hiddenSize];
        outputLayer = new double[1, outputSize];
        random = new System.Random();
        InitializeWeights();
    }

    private void InitializeWeights()
    {
        for (int i = 0; i < inputSize; i++)
        {
            for (int j = 0; j < hiddenSize; j++)
            {
                weightsInputHidden[i, j] = random.NextDouble() - 0.5;
            }
        }

        for (int i = 0; i < hiddenSize; i++)
        {
            for (int j = 0; j < outputSize; j++)
            {
                weightsHiddenOutput[i, j] = random.NextDouble() - 0.5;
            }
        }
    }

    private static double Sigmoid(double x)
    {
        return 1.0 / (1.0 + Exp(-x));
    }

    private static double[, ] Multiply(double[, ] a, double[, ] b)
    {
        int rowsA = a.GetLength(0);
        int colsA = a.GetLength(1);
        int rowsB = b.GetLength(0);
        int colsB = b.GetLength(1);
        double[, ] result = new double[rowsA, colsB];
        for (int i = 0; i < rowsA; i++)
        {
            for (int j = 0; j < colsB; j++)
            {
                double sum = 0;
                for (int k = 0; k < colsA; k++)
                {
                    sum += a[i, k] * b[k, j];
                }

                result[i, j] = sum;
            }
        }

        return result;
    }

    public double[, ] Predict(double[, ] input)
    {
        // Input to hidden layer
        hiddenLayer = Multiply(input, weightsInputHidden);
        for (int i = 0; i < hiddenSize; i++)
        {
            hiddenLayer[0, i] = Sigmoid(hiddenLayer[0, i]);
        }

        // Hidden to output layer
        outputLayer = Multiply(hiddenLayer, weightsHiddenOutput);
        for (int i = 0; i < outputSize; i++)
        {
            outputLayer[0, i] = Sigmoid(outputLayer[0, i]);
        }

        return outputLayer;
    }

    public void Train(double[, ] input, double[, ] target, double learningRate)
    {
        // Forward pass
        Predict(input);
        // Backpropagation
        double[, ] outputError = new double[1, outputSize];
        for (int i = 0; i < outputSize; i++)
        {
            outputError[0, i] = target[0, i] - outputLayer[0, i];
        }

        double[, ] outputDelta = new double[1, outputSize];
        for (int i = 0; i < outputSize; i++)
        {
            outputDelta[0, i] = outputError[0, i] * outputLayer[0, i] * (1 - outputLayer[0, i]);
        }

        double[, ] hiddenError = Multiply(outputDelta, Transpose(weightsHiddenOutput));
        double[, ] hiddenDelta = new double[1, hiddenSize];
        for (int i = 0; i < hiddenSize; i++)
        {
            hiddenDelta[0, i] = hiddenError[0, i] * hiddenLayer[0, i] * (1 - hiddenLayer[0, i]);
        }

        // Update weights
        for (int i = 0; i < inputSize; i++)
        {
            for (int j = 0; j < hiddenSize; j++)
            {
                weightsInputHidden[i, j] += learningRate * hiddenDelta[0, j] * input[0, i];
            }
        }

        for (int i = 0; i < hiddenSize; i++)
        {
            for (int j = 0; j < outputSize; j++)
            {
                weightsHiddenOutput[i, j] += learningRate * outputDelta[0, j] * hiddenLayer[0, i];
            }
        }
    }

    private static double[, ] Transpose(double[, ] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        double[, ] result = new double[cols, rows];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[j, i] = matrix[i, j];
            }
        }

        return result;
    }
}