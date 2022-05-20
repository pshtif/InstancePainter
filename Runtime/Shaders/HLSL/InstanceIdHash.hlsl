half3 CalculateInstanceIdHash(uint instanceID)
{
    #ifdef UNITY_INSTANCING_ENABLED
        return frac(half3(instanceID * 0.123f, instanceID * 0.749f, instanceID * 0.527f));
    #else
        return half3(1, 1, 1);
    #endif
}