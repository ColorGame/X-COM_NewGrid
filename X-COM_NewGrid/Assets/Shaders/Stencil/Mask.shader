
Shader "Custom/StencilMask"
{
    Properties
    {
        [IntRange] _StencilID ("Stencil ID", Range(0, 255)) = 0 // на материале в инспекторе настроить значение Stencil ID
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" "RenderPipeline" = "UniversalPipeline"} // "Queue"="Geometry-1" - очередь на рендер, чтоб значение в буфер записывалось раньше всех

        Pass 
        {
            Blend Zero One
            ZWrite Off // Отключим Z буфер, что бы не видимый объект никого не перекрывал

            Stencil // трафарет
            {
                Ref [_StencilID]
                Comp Always
                Pass Replace // Заменить на экране, где находиться маска, дефолтное значение буфера (0) на значение _StencilID
            }
        }
    }
}
