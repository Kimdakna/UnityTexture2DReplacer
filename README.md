# UnityTexture2DReplacer

Unity IL2CPP 게임에서 로드된 `Texture2D`를 외부 PNG 파일로 런타임에 교체하는 BepInEx 플러그인입니다.

게임의 AssetBundle이나 원본 파일을 직접 수정하지 않고, 실행 중인 Unity의 `Texture2D`를 찾아 동일한 이름의 PNG 파일로 교체합니다.

## Features

- 원본 게임 파일 수정 불필요
- AssetBundle 재패킹 불필요
- `Texture2D.name`을 기준으로 자동 검색 및 교체
- 여러 Texture2D 동시 교체 지원
- 새 텍스처 추가 시 플러그인 재빌드 불필요
- 대소문자를 구분하지 않는 파일명 매칭
- 런타임에 새로 로드되는 Texture2D도 주기적으로 검색
- 이미 교체한 Texture2D 인스턴스는 중복 처리하지 않음

## Requirements

- Windows x64
- Unity IL2CPP 게임
- BepInEx 6 IL2CPP

> 이 플러그인은 BepInEx 6 IL2CPP 환경을 기준으로 제작되었습니다.

## Installation

### 1. BepInEx 설치

대상 게임에 BepInEx 6 IL2CPP를 설치하고 정상적으로 실행되는지 확인합니다.

### 2. 플러그인 설치

`UnityTexture2DReplacer.dll`을 다음 위치에 넣습니다.

```text
<Game Directory>
└─ BepInEx
   └─ plugins
      └─ UnityTexture2DReplacer.dll
```

게임을 한 번 실행하면 다음 폴더가 자동으로 생성됩니다.

```text
BepInEx
└─ plugins
   └─ UnityTexture2DReplacer
      └─ Texture2D
```

직접 생성해도 됩니다.

최종적으로 다음과 같은 구조가 됩니다.

```text
<Game Directory>
└─ BepInEx
   └─ plugins
      ├─ UnityTexture2DReplacer.dll
      │
      └─ UnityTexture2DReplacer
         └─ Texture2D
            ├─ ExampleTexture.png
            ├─ CharacterAtlas.png
            └─ Background01.png
```

## Usage

교체하고 싶은 Unity `Texture2D`의 이름과 동일한 이름의 PNG 파일을 `Texture2D` 폴더에 넣습니다.

예를 들어 게임에 다음 Texture2D가 존재한다면:

```text
Texture2D.name = CharacterAtlas
```

다음 파일을 준비합니다.

```text
BepInEx/plugins/UnityTexture2DReplacer/Texture2D/CharacterAtlas.png
```

게임에서 `CharacterAtlas`가 로드되면 플러그인이 자동으로 해당 Texture2D를 찾아 외부 PNG로 교체합니다.

### Example

게임 내부 Texture2D:

```text
tf_1145001_m0
```

외부 파일:

```text
Texture2D/tf_1145001_m0.png
```

이름이 일치하면 자동으로 교체됩니다.

특정 접두사나 명명 규칙은 필요하지 않습니다.

예를 들어 아래와 같은 이름도 사용할 수 있습니다.

```text
body.png
CharacterAtlas.png
UI_Background_01.png
texture123.png
```

중요한 것은 PNG의 파일명(확장자 제외)이 Unity의 `Texture2D.name`과 일치하는 것입니다.

## Filename Matching

파일명 비교는 대소문자를 구분하지 않습니다.

따라서 다음 파일은 모두 `CharacterAtlas`라는 Texture2D와 매칭될 수 있습니다.

```text
CharacterAtlas.png
characteratlas.png
CHARACTERATLAS.png
```

가능하면 혼동을 방지하기 위해 원본 Texture2D 이름을 그대로 사용하는 것을 권장합니다.

## Texture Size

가능하면 교체 PNG의 해상도를 원본 Texture2D와 동일하게 유지하는 것을 권장합니다.

예:

```text
Original : 2048 x 2048
PNG      : 2048 x 2048
```

특히 Sprite Atlas나 Spine Atlas처럼 UV 좌표를 이용하는 텍스처는 이미지 크기나 레이아웃을 변경하면 잘못 표시될 수 있습니다.

따라서 Atlas를 수정할 때는:

- 이미지 전체 크기를 유지
- 기존 이미지의 위치를 유지
- 필요한 부분의 픽셀만 수정

하는 방식을 권장합니다.

## How It Works

플러그인은 `Texture2D` 폴더에 존재하는 PNG 파일의 이름을 읽어 교체 대상 목록을 만듭니다.

실행 중에는 Unity에 현재 로드되어 있는 `Texture2D`를 검색하고:

```text
PNG filename
        ↓
filename without extension
        ↓
Texture2D.name
        ↓
Match
        ↓
Replace
```

순서로 대상을 찾습니다.

대상이 발견되면 PNG 데이터를 읽고 `ImageConversion.LoadImage()`를 사용하여 현재 게임에서 사용 중인 `Texture2D` 객체에 직접 로드합니다.

따라서 Material, Sprite, Spine Atlas 등이 기존 Texture2D 객체를 참조하고 있어도 참조 자체를 다시 연결할 필요가 없습니다.

## Log

정상적으로 대상을 발견하면 BepInEx 로그에 다음과 비슷하게 표시됩니다.

```text
[TextureReplacer] Target found: CharacterAtlas (2048x2048)
[TextureReplacer] Loading directly into: CharacterAtlas | Before=2048x2048 Format=BC7
[TextureReplacer] DIRECT REPLACED: CharacterAtlas | After=2048x2048 Format=ARGB32
```

BepInEx 로그는 일반적으로 다음 파일에서 확인할 수 있습니다.

```text
BepInEx/LogOutput.log
```

### PNG가 인식되지 않는 경우

다음을 확인하세요.

1. PNG가 올바른 폴더에 있는지 확인합니다.

```text
BepInEx/plugins/UnityTexture2DReplacer/Texture2D
```

2. PNG 파일명과 `Texture2D.name`이 일치하는지 확인합니다.

3. BepInEx 로그에서 다음 메시지가 출력되는지 확인합니다.

```text
[TextureReplacer] Registered: TextureName
```

4. 게임에서 해당 Texture2D가 실제로 로드되었는지 확인합니다.

## Notes

- 현재 PNG 파일만 검색합니다.
- PNG 목록은 플러그인 시작 시 읽습니다.
- 게임 실행 후 PNG를 추가한 경우 게임을 다시 실행해야 목록에 등록됩니다.
- 동일한 Texture2D 인스턴스는 한 번만 교체됩니다.
- 런타임에 새롭게 생성되거나 로드되는 Texture2D를 찾기 위해 일정 간격으로 검색합니다.
- 원본 게임 파일은 변경하지 않습니다.
- 게임 업데이트로 Texture2D 이름이나 Atlas 구조가 변경되면 기존 PNG가 더 이상 정상적으로 적용되지 않을 수 있습니다.

## Building

프로젝트는 BepInEx 6 IL2CPP 환경을 대상으로 합니다.

빌드 시 대상 게임의 BepInEx 환경에서 필요한 어셈블리를 참조해야 합니다.

주요 참조:

```text
BepInEx/core/BepInEx.Core.dll
BepInEx/core/BepInEx.Unity.IL2CPP.dll
BepInEx/core/Il2CppInterop.Runtime.dll

BepInEx/interop/UnityEngine.CoreModule.dll
BepInEx/interop/UnityEngine.ImageConversionModule.dll
```

특히 `UnityEngine.ImageConversionModule.dll`은 일반 Unity Managed DLL이 아니라 대상 IL2CPP 게임에 대해 BepInEx가 생성한 `interop` 버전을 사용해야 합니다.

## Disclaimer

이 프로젝트는 Unity 및 BepInEx의 런타임 동작을 연구하고 Texture2D 교체를 편리하게 하기 위한 범용 도구입니다.

게임 및 소프트웨어의 이용 약관과 관련 법률을 확인하고, 사용에 따른 책임은 사용자 본인에게 있습니다.

## License

라이선스는 저장소의 `LICENSE` 파일을 참고하세요.
