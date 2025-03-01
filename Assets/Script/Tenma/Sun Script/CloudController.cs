using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudController : DayWeatherManager
{
    [Header("Horizon")]
    public GameObject obj;

    void Update()
    {
        //雨の場合
        if (currentWeather == Weather.Rainy)
            obj.SetActive(true);

        //雨以外の場合
        else
            obj.SetActive(false);
    }
}
