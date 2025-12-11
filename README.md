# Visio - Sistema de Streaming RTSP com Processamento de Imagem

Sistema de visualização e processamento de streams RTSP em tempo real com detecção facial e aplicação de filtros usando OpenCV.

## 🚀 Como Rodar o Projeto

### Pré-requisitos
- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 (recomendado) ou VS Code

### Passos

1. **Clone o repositório**
```bash
git clone https://github.com/jaomann/Visio
cd Visio
```

2. **Restaure as dependências**
```bash
dotnet restore
```

3. **Compile o projeto**
```bash
dotnet build -f net8.0-windows10.0.19041.0
```

4. **Execute**
```bash
dotnet run -f net8.0-windows10.0.19041.0
```

Ou abra `Visio.sln` no Visual Studio e pressione F5.

---

## Dependências

### Principais
- **.NET MAUI 8.0** - Framework multiplataforma
- **OpenCvSharp4 (4.9.0.20240103)** - Processamento de imagem
- **CommunityToolkit.Mvvm** - Padrão MVVM

### Testes
- **xUnit** - Framework de testes
- **FluentAssertions** - Assertions legíveis

### Instalação Automática
Todas as dependências são restauradas automaticamente via NuGet durante o build.

---

## Diferenciais Implementados

### 1. Uso de OpenCV

#### Filtros de Imagem
- **Grayscale** - Conversão para escala de cinza
- **Blur** - Suavização gaussiana (kernel 15x15)
- **Edge Detection** - Detecção de bordas com algoritmo Canny

**Código:** `Services/Implementations/ImageProcessingService.cs`

#### Detecção Facial Avançada
- **3 Modelos Haar Cascade:**
  - `haarcascade_frontalface_alt2.xml` (principal - mais preciso)
  - `haarcascade_frontalface_default.xml` (fallback - mais genérico)
  - `haarcascade_profileface.xml` (rostos laterais)

- **5 Otimizações Implementadas:**
  1. **CLAHE** - Equalização adaptativa para luz baixa
  2. **minSize Dinâmico** - Baseado na resolução (width/15 × height/15)
  3. **Buffer Temporal** - Média de 5 frames para tracking suave
  4. **Suavização Exponencial** - Movimento fluido (70/30)
  5. **Cascade em Cascata** - Alt2 → Default → Profile

**Código:** `Services/Implementations/ImageProcessingService.cs` (linhas 59-193)

#### Melhoria de Qualidade
- **CLAHE (Contrast Limited Adaptive Histogram Equalization)**
  - Ativa automaticamente quando brightness < 80
  - Preserva textura facial
  - Melhora detecção em condições de pouca luz

---

### 2. Arquitetura

#### MVVM Completo
- **Views:** XAML puro sem lógica (`Views/MainPage.xaml`)
- **ViewModels:** Lógica de apresentação (`ViewModels/MainViewModel.cs`)
- **Models:** Estruturas de dados (`Models/ImageInfo.cs`)
- **Services:** Lógica de negócio com interfaces

**Separação clara de responsabilidades:**
```
View → ViewModel → Service → OpenCV
```

#### Injeção de Dependência
**Configuração:** `MauiProgram.cs`
```csharp
builder.Services.AddSingleton<IFrameCaptureService, OpenCvFrameCaptureService>();
builder.Services.AddSingleton<IImageProcessingService, ImageProcessingService>();
builder.Services.AddTransient<MainViewModel>();
```

**Benefícios:**
- Testabilidade
- Baixo acoplamento
- Fácil substituição de implementações

#### Projeto Modular
```
Visio/
├── Services/
│   ├── Interfaces/          (Contratos)
│   └── Implementations/     (Implementações)
├── ViewModels/              (Lógica de apresentação)
├── Views/                   (Interface XAML)
├── Models/                  (Dados)
└── Resources/Raw/           (Modelos Haar Cascade)
```

---

### 3. Tratamento de Erros

#### Validação de URL RTSP
```csharp
if (string.IsNullOrWhiteSpace(url))
    return false;

if (!_capture.IsOpened())
{
    ConnectionError?.Invoke(this, "Falha ao abrir stream");
    return false;
}
```

**Código:** `Services/Implementations/OpenCvFrameCaptureService.cs`

#### Mensagens Claras
- **Feedback Visual com Cores:**
  - Verde = "Conectado"
  - Laranja = "Conectando..."
  - Vermelho = "Falha na conexão"
  - Cinza = "Desconectado"

- **Try-Catch em Pontos Críticos:**
  - Conexão RTSP
  - Captura de frames
  - Processamento de imagem
  - Carregamento de cascades

**Código:** `ViewModels/MainViewModel.cs` + `Services/Implementations/`

---

## Uso do Aplicativo

1. **Digite a URL RTSP** (ex: `rtsp://localhost/live`)
2. **Clique em "Conectar"**
3. **Ative filtros desejados:**
   - Grayscale
   - Blur
   - Edge Detection
   - Face Detection
4. **Capture fotos** com o botão de câmera
5. **Visualize na galeria**

---

## Resumo dos Diferenciais

| Categoria | Implementado | Localização |
|-----------|--------------|-------------|
| **OpenCV - Filtros** | 3 filtros | `ImageProcessingService.cs` |
| **OpenCV - Detecção Facial** | 3 Haar Cascades + 5 otimizações | `ImageProcessingService.cs` |
| **OpenCV - Melhoria Qualidade** | CLAHE adaptativo | `ImageProcessingService.cs` |
| **MVVM** | Completo | ViewModels + Views + Services |
| **Injeção de Dependência** | Completo | `MauiProgram.cs` |
| **Projeto Modular** | Completo | Estrutura de pastas |
| **Validação URL** | Completo | `OpenCvFrameCaptureService.cs` |
| **Mensagens de Erro** | Feedback visual com cores | `MainViewModel.cs` |

---

## Autor

João Emanuel Silva
