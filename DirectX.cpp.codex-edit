#include "DirectX.h"
#include "Texture.h"
#include "DebugLog.h"
#include <strsafe.h>

namespace
{
	void ShowDirectXInitError(HWND hWnd, const wchar_t* step, HRESULT hr)
	{
		wchar_t systemMessage[512] = {};
		FormatMessageW(
			FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
			nullptr,
			static_cast<DWORD>(hr),
			MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
			systemMessage,
			ARRAYSIZE(systemMessage),
			nullptr);

		const wchar_t* hint = L"";
		if (hr == DXGI_ERROR_SDK_COMPONENT_MISSING)
		{
			hint =
				L"\nHint: The DirectX debug layer is not installed. "
				L"Install Graphics Tools or disable D3D11_CREATE_DEVICE_DEBUG.";
		}
		else if (hr == E_INVALIDARG)
		{
			hint =
				L"\nHint: A parameter or requested feature level may not be "
				L"supported on this environment.";
		}

		wchar_t message[2048] = {};
		StringCchPrintfW(
			message,
			ARRAYSIZE(message),
			L"DirectX initialization failed.\n\n"
			L"Step: %ls\n"
			L"HRESULT: 0x%08X\n"
			L"Details: %ls%ls",
			step,
			static_cast<unsigned int>(hr),
			systemMessage[0] ? systemMessage : L"No system error description is available.",
			hint);

		MessageBoxW(
			hWnd,
			message,
			L"DirectX Initialization Error",
			MB_OK | MB_ICONERROR);
	}

	HRESULT FailDirectXInit(HWND hWnd, const wchar_t* step, HRESULT hr)
	{
		ShowDirectXInitError(hWnd, step, hr);
		return hr;
	}
}

//--- グローバル変数
ID3D11Device*				g_pDevice;
ID3D11DeviceContext*		g_pContext;
IDXGISwapChain*				g_pSwapChain;
RenderTarget*				g_pRTV;
DepthStencil*				g_pDSV;
ID3D11RasterizerState*		g_pRasterizerState[3];
ID3D11DepthStencilState*	g_pDepthStencilState[2];
ID3D11BlendState*			g_pBlendState[BLEND_MAX];
ID3D11SamplerState*			g_pSamplerState[SAMPLER_MAX];


ID3D11Device* GetDevice()
{
	return g_pDevice;
}
ID3D11DeviceContext* GetContext()
{
	return g_pContext;
}
IDXGISwapChain* GetSwapChain()
{
	return g_pSwapChain;
}
RenderTarget* GetDefaultRTV()
{
	return g_pRTV;
}
DepthStencil* GetDefaultDSV()
{
	return g_pDSV;
}

HRESULT InitDirectX(HWND hWnd, UINT width, UINT height, bool fullscreen)
{
	HRESULT	hr = E_FAIL;
	DXGI_SWAP_CHAIN_DESC sd;
	ZeroMemory(&sd, sizeof(sd));						// ゼロクリア
	sd.BufferDesc.Width = width;						// バックバッファの幅
	sd.BufferDesc.Height = height;						// バックバッファの高さ
	sd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;	// バックバッファフォーマット(R,G,B,A)
	sd.SampleDesc.Count = 1;							// マルチサンプルの数
	sd.BufferDesc.RefreshRate.Numerator = 1000;
	sd.BufferDesc.RefreshRate.Denominator = 1;
	sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;	// バックバッファの使用方法
	sd.BufferCount = 1;									// バックバッファの数
	sd.OutputWindow = hWnd;								// 関連付けるウインドウ
	sd.Windowed = fullscreen ? FALSE : TRUE;
	sd.Flags = DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH;

	// ドライバの種類
	D3D_DRIVER_TYPE driverTypes[] =
	{
		D3D_DRIVER_TYPE_HARDWARE,	// GPUで描画
		D3D_DRIVER_TYPE_WARP,		// 高精度(低速
		D3D_DRIVER_TYPE_REFERENCE,	// CPUで描画
	};
	UINT numDriverTypes = ARRAYSIZE(driverTypes);

	UINT createDeviceFlags = 0;
#if defined(_DEBUG)
	createDeviceFlags |= D3D11_CREATE_DEVICE_DEBUG;
#endif

	// 機能レベル
	D3D_FEATURE_LEVEL featureLevels[] =
	{
		D3D_FEATURE_LEVEL_11_1,		// DirectX11.1対応GPUレベル
		D3D_FEATURE_LEVEL_11_0,		// DirectX11対応GPUレベル
		D3D_FEATURE_LEVEL_10_1,		// DirectX10.1対応GPUレベル
		D3D_FEATURE_LEVEL_10_0,		// DirectX10対応GPUレベル
		D3D_FEATURE_LEVEL_9_3,		// DirectX9.3対応GPUレベル
		D3D_FEATURE_LEVEL_9_2,		// DirectX9.2対応GPUレベル
		D3D_FEATURE_LEVEL_9_1		// Direct9.1対応GPUレベル
	};
	UINT numFeatureLevels = ARRAYSIZE(featureLevels);

	D3D_DRIVER_TYPE driverType;
	D3D_FEATURE_LEVEL featureLevel;
	HRESULT driverResults[] = { E_FAIL, E_FAIL, E_FAIL };

	auto createDeviceAndSwapChain =
		[&](D3D_DRIVER_TYPE type) -> HRESULT
		{
			return D3D11CreateDeviceAndSwapChain(
				NULL,					// ディスプレイデバイスのアダプタ（NULLの場合最初に見つかったアダプタ）
				type,					// デバイスドライバのタイプ
				NULL,					// ソフトウェアラスタライザを使用する場合に指定する
				createDeviceFlags,		// デバイスフラグ
				featureLevels,			// 機能レベル
				numFeatureLevels,		// 機能レベル数
				D3D11_SDK_VERSION,		//
				&sd,					// スワップチェインの設定
				&g_pSwapChain,			// IDXGIDwapChainインタフェース
				&g_pDevice,				// ID3D11Deviceインタフェース
				&featureLevel,			// サポートされている機能レベル
				&g_pContext);			// デバイスコンテキスト
		};

	for (UINT driverTypeIndex = 0; driverTypeIndex < numDriverTypes; ++driverTypeIndex)
	{
		driverType = driverTypes[driverTypeIndex];
		hr = createDeviceAndSwapChain(driverType);

#if defined(_DEBUG)
		// Graphics Tools がない環境ではデバッグレイヤーを外して再試行する。
		if (hr == DXGI_ERROR_SDK_COMPONENT_MISSING &&
			(createDeviceFlags & D3D11_CREATE_DEVICE_DEBUG) != 0)
		{
			createDeviceFlags &= ~D3D11_CREATE_DEVICE_DEBUG;
			hr = createDeviceAndSwapChain(driverType);
		}
#endif

		driverResults[driverTypeIndex] = hr;
		if (SUCCEEDED(hr)) {
			break;
		}
	}
	if (FAILED(hr)) {
		// デバイス単体でも失敗するのか、スワップチェイン生成だけが
		// 失敗するのかを切り分けるための診断。
		HRESULT deviceOnlyResults[] = { E_FAIL, E_FAIL, E_FAIL };
		D3D_FEATURE_LEVEL diagnosticFeatureLevels[] = {
			static_cast<D3D_FEATURE_LEVEL>(0),
			static_cast<D3D_FEATURE_LEVEL>(0),
			static_cast<D3D_FEATURE_LEVEL>(0)
		};

		for (UINT driverTypeIndex = 0;
			driverTypeIndex < numDriverTypes;
			++driverTypeIndex)
		{
			ID3D11Device* diagnosticDevice = nullptr;
			ID3D11DeviceContext* diagnosticContext = nullptr;
			deviceOnlyResults[driverTypeIndex] = D3D11CreateDevice(
				nullptr,
				driverTypes[driverTypeIndex],
				nullptr,
				createDeviceFlags,
				featureLevels,
				numFeatureLevels,
				D3D11_SDK_VERSION,
				&diagnosticDevice,
				&diagnosticFeatureLevels[driverTypeIndex],
				&diagnosticContext);
			SAFE_RELEASE(diagnosticContext);
			SAFE_RELEASE(diagnosticDevice);
		}

		const bool deviceOnlySucceeded =
			SUCCEEDED(deviceOnlyResults[0]) ||
			SUCCEEDED(deviceOnlyResults[1]) ||
			SUCCEEDED(deviceOnlyResults[2]);
		const wchar_t* diagnosis = deviceOnlySucceeded
			? L"Device-only creation succeeded. The failure is narrowed to "
			  L"swap-chain creation. Check OutputWindow and display settings; "
			  L"the requested refresh rate is shown below."
			: L"Device-only creation also failed. The cause is in the D3D "
			  L"device, driver, debug layer, or requested feature levels.";

		wchar_t detailedStep[1200] = {};
		StringCchPrintfW(
			detailedStep,
			ARRAYSIZE(detailedStep),
			L"D3D11CreateDeviceAndSwapChain\n\n"
			L"Combined device + swap-chain results:\n"
			L"  Hardware:  0x%08X\n"
			L"  WARP:      0x%08X\n"
			L"  Reference: 0x%08X\n\n"
			L"Device-only diagnostic results:\n"
			L"  Hardware:  0x%08X (feature level 0x%04X)\n"
			L"  WARP:      0x%08X (feature level 0x%04X)\n"
			L"  Reference: 0x%08X (feature level 0x%04X)\n\n"
			L"Swap chain: %u x %u, %ls, refresh rate %u/%u Hz\n"
			L"Diagnosis: %ls",
			static_cast<unsigned int>(driverResults[0]),
			static_cast<unsigned int>(driverResults[1]),
			static_cast<unsigned int>(driverResults[2]),
			static_cast<unsigned int>(deviceOnlyResults[0]),
			static_cast<unsigned int>(diagnosticFeatureLevels[0]),
			static_cast<unsigned int>(deviceOnlyResults[1]),
			static_cast<unsigned int>(diagnosticFeatureLevels[1]),
			static_cast<unsigned int>(deviceOnlyResults[2]),
			static_cast<unsigned int>(diagnosticFeatureLevels[2]),
			width,
			height,
			fullscreen ? L"Fullscreen" : L"Windowed",
			sd.BufferDesc.RefreshRate.Numerator,
			sd.BufferDesc.RefreshRate.Denominator,
			diagnosis);

		// Hardware の結果を代表HRESULTとして、システム説明にも使用する。
		return FailDirectXInit(hWnd, detailedStep, driverResults[0]);
	}

	//--- レンダーターゲット設定
	g_pRTV = new RenderTarget();
	if (FAILED(hr = g_pRTV->CreateFromScreen()))
		return FailDirectXInit(hWnd, L"RenderTarget::CreateFromScreen", hr);
	g_pDSV = new DepthStencil();
	if (FAILED(hr = g_pDSV->Create(g_pRTV->GetWidth(), g_pRTV->GetHeight(), false)))
		return FailDirectXInit(hWnd, L"DepthStencil::Create", hr);
	SetRenderTargets(1, &g_pRTV, g_pDSV);

	//--- カリング設定
	D3D11_RASTERIZER_DESC rasterizer = {};
	D3D11_CULL_MODE cull[] = {
		D3D11_CULL_NONE,
		D3D11_CULL_FRONT,
		D3D11_CULL_BACK
	};
	const wchar_t* rasterizerSteps[] = {
		L"CreateRasterizerState (CULL_NONE)",
		L"CreateRasterizerState (CULL_FRONT)",
		L"CreateRasterizerState (CULL_BACK)"
	};
	rasterizer.FillMode = D3D11_FILL_SOLID;
	rasterizer.FrontCounterClockwise = false;
	for (int i = 0; i < 3; ++i)
	{
		rasterizer.CullMode = cull[i];
		hr = g_pDevice->CreateRasterizerState(&rasterizer, &g_pRasterizerState[i]);
		if (FAILED(hr)) {
			return FailDirectXInit(hWnd, rasterizerSteps[i], hr);
		}
	}
	//SetCullingMode(D3D11_CULL_BACK);
	SetCullingMode(D3D11_CULL_NONE);

	//--- 深度テスト
	// https://learn.microsoft.com/ja-jp/windows/win32/direct3d11/d3d10-graphics-programming-guide-depth-stencil
	// https://qiita.com/wyt5818956/items/a2a36a1e6c7910512e7a
	D3D11_DEPTH_STENCIL_DESC dsDesc = {};
	// depth test parameter
	dsDesc.DepthEnable = true;
	dsDesc.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ALL;
	dsDesc.DepthFunc = D3D11_COMPARISON_LESS;
	// stencil test parameter
	dsDesc.StencilEnable = true;
	dsDesc.StencilReadMask = 0xFF;
	dsDesc.StencilWriteMask = 0xFF;
	// stencil operations
	dsDesc.FrontFace.StencilFailOp = D3D11_STENCIL_OP_KEEP;
	dsDesc.FrontFace.StencilDepthFailOp = D3D11_STENCIL_OP_INCR;
	dsDesc.FrontFace.StencilPassOp = D3D11_STENCIL_OP_KEEP;
	dsDesc.FrontFace.StencilFunc = D3D11_COMPARISON_ALWAYS;
	dsDesc.BackFace = dsDesc.FrontFace;
	dsDesc.BackFace.StencilFailOp = D3D11_STENCIL_OP_DECR;
	// create state
	hr = g_pDevice->CreateDepthStencilState(&dsDesc, &g_pDepthStencilState[0]);
	if (FAILED(hr)) {
		return FailDirectXInit(
			hWnd,
			L"CreateDepthStencilState (depth enabled)",
			hr);
	}
	dsDesc.DepthEnable = false;
	hr = g_pDevice->CreateDepthStencilState(&dsDesc, &g_pDepthStencilState[1]);
	if (FAILED(hr)) {
		return FailDirectXInit(
			hWnd,
			L"CreateDepthStencilState (depth disabled)",
			hr);
	}
	SetDepthTest(false);

	//--- アルファブレンディング
	// https://pgming-ctrl.com/directx11/blend/
	D3D11_BLEND_DESC blendDesc = {};
	blendDesc.AlphaToCoverageEnable = FALSE;
	blendDesc.IndependentBlendEnable = FALSE;
	blendDesc.RenderTarget[0].BlendEnable = TRUE;
	blendDesc.RenderTarget[0].BlendOp = D3D11_BLEND_OP_ADD;
	blendDesc.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;
	blendDesc.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_ONE;
	blendDesc.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_ZERO;
	blendDesc.RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;
	D3D11_BLEND blend[BLEND_MAX][2] = {
		{D3D11_BLEND_ONE, D3D11_BLEND_ZERO},
		{D3D11_BLEND_SRC_ALPHA, D3D11_BLEND_INV_SRC_ALPHA},
		{D3D11_BLEND_ONE, D3D11_BLEND_ONE},
		{D3D11_BLEND_SRC_ALPHA, D3D11_BLEND_ONE},
		{D3D11_BLEND_ZERO, D3D11_BLEND_INV_SRC_COLOR},
		{D3D11_BLEND_INV_DEST_COLOR, D3D11_BLEND_ONE},
	};
	const wchar_t* blendSteps[BLEND_MAX] = {
		L"CreateBlendState (BLEND_NONE)",
		L"CreateBlendState (BLEND_ALPHA)",
		L"CreateBlendState (BLEND_ADD)",
		L"CreateBlendState (BLEND_ADDALPHA)",
		L"CreateBlendState (BLEND_SUB)",
		L"CreateBlendState (BLEND_SCREEN)"
	};
	for (int i = 0; i < BLEND_MAX; ++i)
	{
		blendDesc.RenderTarget[0].SrcBlend = blend[i][0];
		blendDesc.RenderTarget[0].DestBlend = blend[i][1];
		hr = g_pDevice->CreateBlendState(&blendDesc, &g_pBlendState[i]);
		if (FAILED(hr)) {
			return FailDirectXInit(hWnd, blendSteps[i], hr);
		}
	}
	SetBlendMode(BLEND_ALPHA);

	// サンプラー
	D3D11_SAMPLER_DESC samplerDesc = {};
	D3D11_FILTER filter[] = {
		D3D11_FILTER_MIN_MAG_MIP_LINEAR,
		D3D11_FILTER_MIN_MAG_MIP_POINT,
	};
	const wchar_t* samplerSteps[SAMPLER_MAX] = {
		L"CreateSamplerState (SAMPLER_LINEAR)",
		L"CreateSamplerState (SAMPLER_POINT)"
	};
	samplerDesc.AddressU = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.AddressV = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
	for (int i = 0; i < SAMPLER_MAX; ++i)
	{
		samplerDesc.Filter = filter[i];
		hr = g_pDevice->CreateSamplerState(&samplerDesc, &g_pSamplerState[i]);
		if (FAILED(hr)) {
			return FailDirectXInit(hWnd, samplerSteps[i], hr);
		}
	}
	SetSamplerState(SAMPLER_LINEAR);

	DebugLog::Init();

	return S_OK;
}

void UninitDirectX()
{
	SAFE_DELETE(g_pDSV);
	SAFE_DELETE(g_pRTV);

	for (int i = 0; i < SAMPLER_MAX; ++i)
		SAFE_RELEASE(g_pSamplerState[i]);
	for (int i = 0; i < BLEND_MAX; ++i)
		SAFE_RELEASE(g_pBlendState[i]);
	for (int i = 0; i < 2; ++i)
		SAFE_RELEASE(g_pDepthStencilState[i]);
	for(int i = 0; i < 3; ++ i)
		SAFE_RELEASE(g_pRasterizerState[i]);
	if(g_pContext)
		g_pContext->ClearState();
	SAFE_RELEASE(g_pContext);
	if(g_pSwapChain)
		g_pSwapChain->SetFullscreenState(false, NULL);
	SAFE_RELEASE(g_pSwapChain);

	DebugLog::Uninit(g_pDevice);
	SAFE_RELEASE(g_pDevice);
}

void BeginDrawDirectX()
{
	float color[4] = { 0.8f, 0.9f, 1.0f, 1.0f };
	g_pRTV->Clear(color);
	g_pDSV->Clear();
}
void EndDrawDirectX()
{
	g_pSwapChain->Present(0, 0);
}



void SetRenderTargets(UINT num, RenderTarget** ppViews, DepthStencil* pView)
{
	static ID3D11RenderTargetView* rtvs[4];

	if (num > 4) num = 4;
	for (UINT i = 0; i < num; ++i)
		rtvs[i] = ppViews[i]->GetView();
	g_pContext->OMSetRenderTargets(num, rtvs, pView ? pView->GetView() : nullptr);

	// ビューポートの設定
	D3D11_VIEWPORT vp;
	vp.TopLeftX = 0.0f;
	vp.TopLeftY = 0.0f;
	vp.Width = (float)ppViews[0]->GetWidth();
	vp.Height = (float)ppViews[0]->GetHeight();
	vp.MinDepth = 0.0f;
	vp.MaxDepth = 1.0f;
	g_pContext->RSSetViewports(1, &vp);
}

void SetCullingMode(D3D11_CULL_MODE cull)
{
	switch (cull)
	{
	case D3D11_CULL_NONE: g_pContext->RSSetState(g_pRasterizerState[0]); break;
	case D3D11_CULL_FRONT: g_pContext->RSSetState(g_pRasterizerState[1]); break;
	case D3D11_CULL_BACK: g_pContext->RSSetState(g_pRasterizerState[2]); break;
	}
}
void SetDepthTest(bool enable)
{
	g_pContext->OMSetDepthStencilState(
		g_pDepthStencilState[enable ? 0 : 1], 1);
}

void SetDepthTest(bool enable, bool writeEnable)
{
	ID3D11DepthStencilState* s =
		(!enable) ? g_pDepthStencilState[1] :
		(writeEnable ? g_pDepthStencilState[0] : g_pDepthStencilState[2]);

	g_pContext->OMSetDepthStencilState(s, 1);
}

void SetBlendMode(BlendMode blend)
{
	if (blend < 0 || blend >= BLEND_MAX) return;
	FLOAT blendFactor[4] = { D3D11_BLEND_ZERO, D3D11_BLEND_ZERO, D3D11_BLEND_ZERO, D3D11_BLEND_ZERO };
	g_pContext->OMSetBlendState(g_pBlendState[blend], blendFactor, 0xffffffff);
}
void SetSamplerState(SamplerState state)
{
	if (state < 0 || state >= SAMPLER_MAX) return;
	g_pContext->PSSetSamplers(0, 1, &g_pSamplerState[state]);
}
