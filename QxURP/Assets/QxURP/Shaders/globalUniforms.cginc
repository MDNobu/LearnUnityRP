
sampler2D _GT0;
sampler2D _GT1;
sampler2D _GT2;
sampler2D _GT3;
sampler2D _gdepth;
sampler2D _hizBuffer;

samplerCUBE _diffuseIBL;
samplerCUBE _specularIBL;
sampler2D   _brdfLut;

sampler2D _shadowTex0;
sampler2D _shadowTex1;
sampler2D _shadowTex2;
sampler2D _shadowTex3;
sampler2D _shadowStrength;
sampler2D _shadoMask;


sampler2D _noiseTex;

float _screenWidth;
float _screenHeight;
float _noiseTexResolution;

float _far;
float _near;


// 这几个变量表示不同split的光锥的宽度，用来将world space转换到shadow uv
float _orthoWidth0;
float _orthoWidth1;
float _orthoWidth2;
float _orthoWidth3;

// 这个是正交的光源视锥的forward的长度的一半
float _orthoDistance;
float _shadowmapResolution;

float _usingShadowMask;

// shading point 偏移这么多比例的法线
float _shadowNormalBias0;
float _shadowNormalBias1;
float _shadowNormalBias2;
float _shadowNormalBias3;


float4x4 _vpMatrix;
float4x4 _vpMatrixInv;
float4x4 _vpMatrixPrev;
float4x4 _vpMatrixPrevInv;

float4x4 _shadowVpMatrix0;
float4x4 _shadowVpMatrix1;
float4x4 _shadowVpMatrix2;
float4x4 _shadowVpMatrix3;

float _split0;
float _split1;
float _split2;
float _split3;


float _depthNormalBias0;
float _depthNormalBias1;
float _depthNormalBias2;
float _depthNormalBias3;

float _pcssSearchRadius0;
float _pcssSearchRadius1;
float _pcssSearchRadius2;
float _pcssSearchRadius3;

float _pcssFilterRadius0;
float _pcssFilterRadius1;
float _pcssFilterRadius2;
float _pcssFilterRadius3;

// cluster light 参数
float _numClusterX;
float _numClusterY;
float _numClusterZ;

struct PointLight
{
    float3 color;
    float intensity;
    float3 position;
    float radius;
};

struct LightIndex
{
    int count;
    int start;
};

StructuredBuffer<PointLight> _lightBuffer;
StructuredBuffer<uint> _lightAssignBuffer;
StructuredBuffer<LightIndex> _assignTable;