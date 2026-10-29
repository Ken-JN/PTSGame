using System;
using UnityEngine;

public class BelajarDelegate : MonoBehaviour
{
    private delegate void SimpleAction();

    void Start()
    {
        ExampleAction1();
        ExampleAction2();
        ExampleAction3();
    }

    void Update()
    {
    }

    void ExampleAction1()
    {
        SimpleAction action = WriteHello;
        action();
    }

    void ExampleAction2()
    {
        SimpleAction action = WriteHello;
        action += WriteWorld;
        action();
    }

    void ExampleAction3()
    {
        Action action = WriteHello;
        action += WriteSigma;
        action();
    }

    void WriteWorld()
    {
        Debug.Log("World");
    }

    void WriteHello()
    {
        Debug.Log("Hello");
    }

    void WriteSigma()
    {
        Debug.Log("Sigma");
    }
}
