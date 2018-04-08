using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class EventManager : MonoBehaviour
{
    private readonly Dictionary<Type, object> _eventDictionary = new Dictionary<Type, object>();

    private static EventManager _eventManager;

    // TODO: might be worth thinking about alternative DI strategies here
    public static EventManager Instance
    {
        get
        {
            if (_eventManager) return _eventManager;

            _eventManager = FindObjectOfType<EventManager>();

            if (!_eventManager)
            {
                throw new Exception("There needs to be one active EventManager script on a GameObject in your scene.");
            }

            return _eventManager;
        }
    }

    public void StartListening<T>(Action<T> listener)
    {
        if (_eventManager == null)
        {
            Debug.LogError("Tried to start listening on a null EventManager");
            return;
        }

        object handlers;
        List<Action<T>> typedHandlers;
        if (_eventDictionary.TryGetValue(typeof(T), out handlers))
        {
            typedHandlers = (List<Action<T>>) handlers;
            typedHandlers.Add(listener);
        }
        else
        {
            typedHandlers = new List<Action<T>> {listener};
            _eventDictionary.Add(typeof(T), typedHandlers);
        }
    }

    public void StopListening<T>(Action<T> listener)
    {
        if (_eventManager == null)
        {
            Debug.LogWarning("Tried to stop listening on null EventManager - are we shutting down?");
            return;
        }
        object handlers;
        if (_eventDictionary.TryGetValue(typeof(T), out handlers))
        {
            var typedHandlers = (List<Action<T>>) handlers;
            typedHandlers.RemoveAll(l => listener == l);
        }
    }

    public void TriggerEvent<T>(T payload)
    {
        if (_eventManager == null)
        {
            Debug.LogError("Tried to trigger action on null EventManager");
            return;
        }
        var type = typeof(T);
        var typeName = type.Name;
        object handlers;
        if (_eventDictionary.TryGetValue(type, out handlers))
        {
            var typedHandlers = (List<Action<T>>) handlers;
            Debug.Log("Triggered event type "+typeName+" for " + typedHandlers.Count + " listeners");
            foreach (var handler in typedHandlers)
            {
                handler.Invoke(payload);
            }
        }
        else
        {
            Debug.LogWarning("Triggered event type "+typeName + " for 0 listeners");
        }
    }
}
