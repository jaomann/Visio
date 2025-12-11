# Manual do Usuário - Visio

## Visão Geral

O Visio é um aplicativo de visualização e processamento de streams RTSP em tempo real. Ele permite conectar-se a câmeras IP, aplicar filtros de imagem e detectar rostos automaticamente.

---

## Tela Principal

### Conectando ao Stream RTSP

1. **Digite a URL do stream**
   - Localize o campo de texto na parte superior da tela
   - Digite a URL RTSP da sua câmera
   - Formato: `rtsp://[endereço]:[porta]/[caminho]`
   - Exemplo: `rtsp://localhost/live`

2. **Clique no botão "Conectar"**
   - O status mudará para "Conectando..." (laranja)
   - Aguarde alguns segundos
   - Se conectado com sucesso: status "Conectado" (verde)
   - Se falhar: status "Falha na conexão" (vermelho)

3. **Visualize o stream**
   - O vídeo aparecerá na área central da tela

### Aplicando Filtros

O Visio oferece 4 filtros que podem ser ativados/desativados em tempo real:

#### Grayscale (Escala de Cinza)
- **O que faz:** Converte a imagem para preto e branco
- **Como usar:** Marque a caixa "Grayscale"
- **Uso:** Reduz informação de cor, útil para análise de contraste

#### Blur (Desfoque)
- **O que faz:** Aplica suavização gaussiana na imagem
- **Como usar:** Marque a caixa "Blur"
- **Uso:** Reduz ruído e detalhes, cria efeito artístico

#### Edge Detection (Detecção de Bordas)
- **O que faz:** Destaca contornos e bordas usando algoritmo Canny
- **Como usar:** Marque a caixa "Edge Detection"
- **Uso:** Análise estrutural, identificação de formas

#### Face Detection (Detecção Facial)
- **O que faz:** Detecta e rastreia rostos em tempo real
- **Como usar:** Marque a caixa "Face Detection"
- **Recursos:**
  - Desenha retângulo verde ao redor do rosto
  - Marca o centro do rosto com ponto vermelho
  - Tracking suave (sem tremor)
  - Funciona em diferentes iluminações
  - Detecta rostos frontais e laterais

**Dica:** Você pode combinar múltiplos filtros simultaneamente!

### Capturando Fotos

1. **Clique no botão de câmera** (ícone de câmera fotográfica)
2. A foto será salva automaticamente
3. **Localização:** `C:\Users\[seu-usuario]\Pictures\Visio\`
4. **Nome do arquivo:** `snapshot_AAAAMMDD_HHMMSS.png`
   - Exemplo: `snapshot_20231211_153045.png`

### Desconectando

1. **Clique no botão "Desconectar"**
2. O stream para imediatamente
3. Status volta para "Desconectado" (cinza)
4. A imagem congela no último frame

---

## Galeria

### Acessando a Galeria

1. Clique na aba "Galeria" na parte inferior
2. Todas as fotos capturadas serão exibidas

### Visualizando Fotos

- As fotos são exibidas em miniatura
- Clique em uma foto para visualizar em tamanho maior
- Use os botões de navegação para alternar entre fotos

### Excluindo Fotos

1. Selecione a foto desejada
2. Clique no botão "Excluir"
3. Confirme a exclusão
4. A foto será removida permanentemente

---

## Solução de Problemas

### "Falha na conexão"

**Possíveis causas:**
- URL RTSP incorreta
- Servidor RTSP não está rodando
- Problemas de rede/firewall
- Credenciais necessárias (não suportadas atualmente)

**Soluções:**
1. Verifique se a URL está correta
2. Teste a URL em um player de vídeo (VLC)
3. Confirme que o servidor está acessível
4. Verifique configurações de firewall

### "Nenhum frame disponível"

**Possíveis causas:**
- Stream ainda não iniciou
- Problemas de rede
- Stream encerrado pelo servidor

**Soluções:**
1. Aguarde alguns segundos após conectar
2. Desconecte e reconecte
3. Verifique se o servidor está transmitindo

### Detecção facial não funciona

**Possíveis causas:**
- Rosto não está visível
- Iluminação muito baixa ou muito alta
- Rosto muito pequeno ou muito grande
- Ângulo muito lateral

**Soluções:**
1. Posicione o rosto de frente para a câmera
2. Melhore a iluminação do ambiente
3. Ajuste a distância da câmera
4. Certifique-se que o rosto está claramente visível

### Aplicativo lento ou travando

**Possíveis causas:**
- Resolução muito alta do stream
- Múltiplos filtros ativos simultaneamente
- Hardware insuficiente

**Soluções:**
1. Use stream com resolução menor (640x480 ou 1280x720)
2. Desative filtros não utilizados
3. Feche outros aplicativos pesados

---

## Dicas de Uso

### Melhor Qualidade de Detecção Facial

1. **Iluminação:** Use iluminação frontal uniforme
2. **Posição:** Mantenha o rosto de frente para a câmera
4. **Movimento:** Evite movimentos bruscos
5. **Resolução:** Use pelo menos 640x480

### Performance Otimizada

1. **Resolução:** 1280x720 é ideal (balanço qualidade/performance)
2. **Filtros:** Use apenas os necessários
3. **FPS:** 15-30 FPS é suficiente para maioria dos casos

### URLs RTSP Comuns

**Câmeras IP genéricas:**
- `rtsp://[ip]:554/stream1`
- `rtsp://[ip]:8554/live`
- `rtsp://admin:senha@[ip]:554/cam/realmonitor`

**Servidores locais:**
- MediaMTX: `rtsp://localhost:8554/[nome-stream]`
- VLC: `rtsp://localhost:8554/`
- FFmpeg: `rtsp://localhost:8554/stream`

---

## Requisitos do Sistema

### Mínimos
- Windows 10 (64-bit)
- 4GB RAM
- Processador dual-core 2.0 GHz
- Conexão de rede estável

### Recomendados
- Windows 11 (64-bit)
- 8GB RAM
- Processador quad-core 2.5 GHz+
- Conexão de rede de alta velocidade
