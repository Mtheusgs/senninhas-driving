# Senninhas Driving — Menu, Garagem, Modo Aula e Ajuste — Design

Data: 2026-11-04
Complementa `2026-09-30-f1-hillclimb-design.md` (o jogo F1 Hill Climb). Esta entrega acontece **entre a etapa 1
(base jogável, pronta) e a etapa 2 (iluminação)**. Revoga daquela spec o item de fora de escopo "Menu/tela de título"
e antecipa para cá a parte básica do "Modo aula", que era da etapa 6.

## Por quê

Depois de jogar a etapa 1, o aluno pediu:
- **Ajuste:** "capotar tá quase impossível, morros muito suaves"; aceleração e combustível estão bons.
- **Menu completo**, com escolha da **cor do carro**.
- **Modo aula** que mostre na tela cada técnica de CG, para narrar o vídeo do trabalho.
- **Modo Lua** "com gravidade bugada que nem no jogo real" (Hill Climb Racing): carro flutuando e voando longe.

O jogo é a entrega de Computação Gráfica: o menu e o modo aula têm que deixar claro, para o professor, onde cada
técnica está — sem atrapalhar quem só quer jogar.

## Objetivos

1. Jogo mais desafiador: morros íngremes e capotar possível (mas não frustrante).
2. Menu principal com título **Senninhas Driving**, fundo animado e as telas: Jogar, Garagem, Controles,
   Configurações, Técnicas de CG, Recorde, Sair.
3. Garagem com escolha de cor do F1, salva entre sessões.
4. Modo aula mostrando as técnicas já existentes no jogo, ligável por tecla e pelo menu.
5. Pausa e fim de jogo com caminho de volta ao menu.
6. Modo Lua jogável, escolhido no menu ao lado do modo Terra.

**Não-objetivos:** loja, upgrades, vários carros, mapas além de Terra e Lua, ranking online, gamepad/touch.

**Anti-objetivos** (falha mesmo que "funcione"):
- Botão ou opção que não faz nada (por exemplo, opção de modo aula que não mostra nada).
- Modo aula ligado por padrão para quem só quer jogar, ou que deixe o jogo ilegível.
- Trocar a cor tingindo pneus, piloto, asas pretas e textos.
- Recorde, cor ou configurações apagados ao reiniciar o jogo.

## Restrições

- Unity 6000.3.25f1, URP 2D, uGUI + TextMeshPro, Input Manager antigo (`Input.GetKey`) — o que o projeto já usa.
- As cenas continuam montadas por script (`Editor/MontarJogo.cs` e um novo `Editor/MontarMenu.cs`), para poderem
  ser refeitas sem perder ajustes.
- Algoritmos de CG continuam em funções puras testadas (`Raster`, `TerrenoGerador`, `TerrenoChunk`); o modo aula
  só **desenha** dados que essas funções já produzem, não reimplementa nada.
- Persistência em `PlayerPrefs`. Testes não podem gravar nas chaves reais (mesmo cuidado já usado com o recorde).
- A cena do vídeo `Main.unity` continua no build e funcionando.

## Ajuste de jogabilidade

- **Morros:** amplitude inicial e máxima maiores e crescimento mais rápido com a distância. Alvo: nos primeiros
  ~100 m ainda tranquilo; a partir de ~300 m, morros com inclinação perto de 45° (≈ 1,0). O limite testado de
  inclinação jogável (≤ 1,5) e o de x crescente continuam valendo.
- **Fundo do terreno:** com morros mais altos, a janela do trecho precisa continuar cobrindo a câmera no zoom
  máximo (o teste `Trecho_FundoCobreACameraNoZoomMaximo` continua valendo). Se a altura da janela de recorte
  aumentar, tudo bem; se o recorte passar a cortar, melhor ainda para o modo aula.
- **Capotar:** mais torque no ar e centro de massa menos baixo. Alvo: segurando → no ar por ~1 s o carro gira mais
  de 90°; em subida forte acelerando, é possível empinar e capotar.
- Valores finais ficam no Inspector/`MontarJogo` e são ajustados jogando.
- Aceleração, velocidade e combustível: **não mudam**.

## Cenas e fluxo

```
[Menu] --Jogar: Terra | Lua--> [Jogo] --Esc--> [Pausa: Continuar | Reiniciar | Menu principal]
   ^                 |
   |                 +--fim--> [Fim: Jogar de novo | Menu]
   +------ Menu -----+
```

- Nova cena `Menu.unity`, primeira do build; depois `Jogo.unity`, depois `Main.unity`.
- Abrir o jogo cai no menu. "Jogar" pergunta **Terra** ou **Lua** e carrega `Jogo.unity` no modo escolhido
  (uma cena só, configurada pelo modo). R e "Jogar de novo" reiniciam no mesmo modo.

## Menu principal

- **Fundo animado:** o próprio terreno infinito com um F1 dirigindo sozinho ("piloto automático": acelera, alivia
  em subidas fortes para não capotar com frequência; se capotar ou parar, o demo reinicia em silêncio). Câmera um
  pouco mais afastada que no jogo. Sem HUD.
- **Título** "Senninhas Driving" em destaque, com as opções empilhadas: Jogar, Garagem, Controles, Configurações,
  Técnicas de CG, Recorde, Sair.
- **Navegação:** mouse e teclado (setas/Enter/Esc). Esc em uma subtela volta à tela principal.
- **Sair:** fecha o aplicativo (no Editor, sai do Play Mode).

### Garagem

- Fileira de ~8 cores (incluindo vermelho, azul, verde, laranja, amarelo, prata, preto, rosa).
- O F1 grande no centro "girando" como numa vitrine: a escala x oscila entre +1 e −1 (o carro aparece espelhado
  ao passar pelo meio — espelhamento como efeito visual).
- A cor escolhida fica salva e é aplicada ao F1 do jogo e ao do fundo do menu.
- Só a **pintura** muda: rodas, piloto, halo, asas pretas, faixa, número e "CG RACING" ficam como estão. Para isso,
  `Tools/gerar_f1.py` passa a gerar a pintura numa camada própria (clara, com o sombreado em tons de cinza) que é
  tingida pela cor; o resto fica numa camada de detalhes por cima.
- *Decisão do designer:* entram na pintura tudo que hoje é vermelho — chassi, cobertura do motor e as duas placas
  laterais das asas — e o contorno escuro delas (em cinza escuro, para tingir como um tom mais escuro da cor).
- *Decisão do designer:* o collider do chassi continua com a forma atual: o `f1_corpo.png` completo continua sendo
  gerado e é dele que sai o `PolygonCollider2D`; as camadas novas são só para desenhar.

### Controles

- Tela com as teclas: → acelera, ← freia/ré, no ar → e ← giram, R reinicia, Esc pausa, Tab modo aula, F scanline
  em câmera lenta (no modo aula).

### Configurações

- Tela cheia, **modo aula ao iniciar** (liga/desliga) e **qualidade** com 3 níveis que mudam algo visível:
  Alta (terreno a 64 px/unidade), Média (48), Baixa (32, para PCs fracos). Tudo salvo.
- *Decisão do designer:* o volume só aparece na etapa 6, quando houver som — antes disso seria uma opção que não
  faz nada (anti-objetivo). Os 6 níveis de qualidade do projeto não diferem visivelmente neste jogo 2D, por isso a
  opção usa os 3 níveis acima.

### Técnicas de CG

- Uma tela com as **9 linhas** da tabela de técnicas da spec principal. Cada linha lista os lugares onde a técnica
  aparece, e **cada lugar** tem seu selo "no jogo" ou "em breve" (uma técnica pode estar parcialmente pronta).
- Estado hoje (*decisão do designer*, pelo que o código realmente faz):
  - Rasterização: Bresenham na intro e no minimapa — **em breve** (`Raster.Line` existe mas ainda não aparece no jogo).
  - Preenchimento: scanline do terreno — **no jogo**; regiões do F1 na intro e sombra — em breve.
  - Curva de Bézier: terreno — **no jogo**.
  - Recorte: Sutherland–Hodgman no terreno — **no jogo**; reflexo no lago e Cohen–Sutherland no minimapa — em breve.
  - Translação, Rotação: carro, coletáveis, giro no ar — **no jogo**; parallax e wireframe — em breve.
  - Espelhamento: emenda C1 e garagem — **no jogo**; reflexo no lago — em breve.
  - Projeções, Iluminação e sombreamento — em breve.
- O selo é a fonte da verdade: quando uma etapa futura entregar a técnica, o selo muda junto.

### Recorde

- Por modo (Terra e Lua): melhor distância e total de moedas da melhor partida. Botão para zerar os recordes,
  com confirmação.

## Modo Lua

- **Gravidade bem mais baixa** (alvo: uns 20–25% da Terra), como no Hill Climb: o carro flutua, sobe alto nos
  morros e gira muito no ar. O giro no ar pode ser mais forte que na Terra para dar a sensação "bugada".
- **Terreno mais acidentado** que o da Terra desde o início (mais saltos), ainda dentro dos limites testados
  de inclinação jogável e x crescente. Mesma geração por Bézier, recorte e scanline; outra semente.
- **Visual:** chão cinza de poeira lunar (paleta própria no lugar de grama/terra), céu preto com estrelas,
  a Terra pendurada no céu. Moedas e galões iguais.
- **Combustível:** mesmas regras (o aluno aprovou o consumo atual).
- Na etapa 2 (iluminação), a Lua não tem ciclo dia/noite: luz fixa do Sol, sombras duras. Fica registrado aqui para
  o plano da etapa 2.

## No jogo

- **Pausa (Esc):** Continuar, Reiniciar, Menu principal.
- **Fim:** o painel atual ganha os botões Jogar de novo e Menu. R continua funcionando.
- Corrigir de passagem (já levantado na revisão da etapa 1): moedas tocadas depois do fim não contam; giro no ar
  não funciona depois do fim.

## Modo aula (Tab no jogo; padrão vem das Configurações)

Sobreposição desenhada por cima do jogo, sem mudar a física nem a jogabilidade. Mostra:

- **Curva de Bézier:** pontos de controle P0–P3 e polígono de controle de cada trecho visível.
- **Espelhamento na emenda:** nas junções, o P2 do trecho anterior, a junção e o P1 do próximo alinhados, com uma
  marca indicando que P1 é o espelho de P2 em torno da junção.
- **Recorte (Sutherland–Hodgman):** a janela de recorte de cada trecho (amarelo), o polígono antes do recorte
  (vermelho) e o polígono recortado (verde).
  - *Decisão do designer:* para o recorte cortar **sempre** (e não só em vales raros), o polígono do trecho passa a
    descer bem abaixo da janela (fundo muito profundo); a janela continua do tamanho que cobre a câmera. Assim o
    vermelho aparece passando da borda de baixo da janela e o verde termina nela, em todo trecho. Textura e
    collider não mudam.
- **Preenchimento (scanline):** tecla **F** congela o jogo (como uma pausa), apaga o trecho que está no centro da
  tela e o repinta linha a linha em ~2 s, usando as mesmas linhas (spans) do preenchimento real; depois o jogo
  continua. O collider não muda durante a animação. Só funciona com o modo aula ligado e fora da pausa/fim.
- **Translação e rotação:** eixos locais do F1 (x e y) acompanhando o carro e o vetor de velocidade.
- **Rótulo no canto:** "Modo aula" + lista das técnicas visíveis agora.

Cada etapa futura acrescenta a sua parte (normais/sol, minimapa/projeção, água/reflexo, wireframe da intro).

## Testes esperados

- Partes puras com teste EditMode, como até agora (por exemplo: paleta de cores e persistência das escolhas,
  geração das linhas do modo aula a partir de um trecho, novos parâmetros do terreno ainda dentro dos limites).
- Testes PlayMode do fluxo: o jogo abre no menu; Jogar carrega o jogo no modo escolhido (na Lua, gravidade baixa
  e paleta lunar); recorde separado por modo; a cor escolhida aparece no F1;
  Pausa → Menu principal volta ao menu; Fim → Jogar de novo reinicia; Tab liga e desliga a sobreposição;
  configurações sobrevivem a recarregar a cena; nenhum teste grava nas chaves reais de `PlayerPrefs`.
- Os testes atuais (48 EditMode, 7 PlayMode) continuam passando, ajustados onde o fluxo mudou (por exemplo, a cena
  de entrada passa a ser o menu).
- Capturas automáticas: menu, garagem com outra cor, jogo com o modo aula ligado, jogo na Lua.

## Deixado para quem constrói

- Layout exato, fontes, cores do menu e animações de transição (coerentes com o HUD atual: caixas escuras
  translúcidas, texto branco em negrito).
- Valores exatos de terreno, torque e centro de massa dentro dos alvos acima.
- Como desenhar a sobreposição (LineRenderers, GL, malhas), desde que fique nítida no vídeo e não pese no jogo.
- Lógica exata do piloto automático do menu.
- Valor exato da gravidade lunar e do giro no ar na Lua, dentro do alvo.
- Se o fundo do menu mostra Terra ou alterna entre Terra e Lua.
