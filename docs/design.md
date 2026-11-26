# CS2 Mod 介绍站 · 设计系统

> 对象：《都市：天际线 II》mod `CS2_Unlimited_PubilcVehicle`（公共交通配车上限解锁）的单模组介绍站
> 风格目标：和 `mods.paradoxplaza.com` 的 CS2 模组详情页属于同一审美家族，但它是**一个独立的单模组介绍站**，不是平台页的复制品
> 版本：v2　日期：2026-11-17
> v2 说明：对官方站做了二次实测（无头浏览器渲染 + 抓取 5 个懒加载 CSS 块），**修正了 v1 中 12 处与官方站不符的判断**。清单见第 18 节。

---

## 0. 怎么读这份文档

| 标记 | 含义 |
|---|---|
| **[实测]** | 从官方站运行时 CSS 直接读到的声明值，附选择器，可复核 |
| **[自定]** | 本站新做的决定，不来自官方站 |
| **[计算]** | 按 WCAG 公式手算的对比度，上线前用工具复核 |
| **[README]** | 来自 mod 仓库 README 的事实 |

文档按"先立原则，再给 token，再落到组件和页面"排列。**第 1、2、8 节最重要**，色值可以改，这三节的取舍不要轻易动。

**第 3–6 节的值已全部换成实测值**，v1 里那些「目测」已经没有了。

---

## 1. 定位与设计原则

### 1.1 这个站是什么

- **一句话**：给玩家看的 mod 说明书，让人在一分钟内知道它做什么、能不能用、怎么装。
- **受众**：CS2 玩家，中文为主、英文对等。他们熟悉 Paradox Mods 的页面习惯。
- **主要任务**：说清楚功能 → 说清楚当前状态 → 引导获取。
- **不是**：营销落地页，也不是开发者文档站。技术细节放折叠区或链接到仓库。

### 1.2 七条原则

1. **像产品说明书，不像落地页。** 右栏是规格表，信息密度优先于留白。[官方站]
2. **一个框架，一层主题。** 中性框架立住，再用 4 个主题变量做 CS2 的品牌化。[官方站]
3. **全页只有一个主按钮。** 其余都是描边或文字。视觉焦点不竞争。[官方站]
4. **靠层级底色和 1px 描边分区，不靠投影。** 官方用半透明叠加做层次，本站用同位置的**实色对应值**替代（理由见 3.5）。[自定，改自 v1]
5. **大写小标签只用在"有结构意义"的地方**：规格表的键、区块头。不要在每个标题上方都叠一个。[自定]
6. **状态诚实高于好看。** 这个 mod 没做完游戏内验收，网站的每一处文案和视觉都不能暗示"已稳定可用"。见第 8 节。[自定]
7. **没有素材就不装有素材。** 没有真实游戏内截图时，用明确标注的示意图，不用假截图、不用官方宣传图。[自定]

---

## 2. 三层结构

```
第 3 层  内容层     mod 自己的截图、描述、设置表 —— 作者的内容，框架不干预
第 2 层  主题层     4 个 CSS 变量：背景 + 主色 + 主色文字 + 半透明主色
第 1 层  框架层     深色中性底 + 单一字体族 + 固定容器 + 固定组件
```

本站只有一个游戏，但仍保留主题层。理由：以后换主题色、做节日版本或复用给下一个 mod，只改 4 个变量。

---

## 3. 色彩

### 3.0 官方站的真实做法（先看这个，否则下面的 token 会被误读）

官方详情页**没有"面板底色"这个色值**。分层全靠**半透明叠加 + 毛玻璃**，叠在游戏键图上：

```css
/* .src-components-Mods-Details-styles__root  [实测] */
background: var(--gamePageBackgroundColorTransparent);   /* #0D243940 */
backdrop-filter: blur(16px) contrast(0.75) brightness(0.5);
border: 1px solid rgba(255, 255, 255, 0.2);
border-radius: 8px;
box-shadow: 0 4px 8px 2px rgba(0, 0, 0, 0.2);
padding: 16px;
gap: 32px;
display: flex;
```

侧栏卡片（`.src-components-Mods-Details-SideCard-styles__root` [实测]）：

```css
background: rgba(255, 255, 255, 0.06);
border: 1px solid rgba(255, 255, 255, 0.05);
border-radius: 4px;
padding: 16px;
margin-top: 24px;
box-shadow: 0 0 4px 6px rgba(0, 0, 0, 0.03);
```

规格表的斑马纹是第三层（`rgba(0,0,0,0.25)`），标签 chip 是第四层（`rgba(0,0,0,0.3)`）。

**所以 v1 里"面板 `#1f242d`"和"全站无阴影"两条都是错的**，`#2c2c2c` 也从来不是面板色。

### 3.1 本站的框架层（换算成实色）

本站没有游戏键图做背景，毛玻璃叠在纯色上没有意义，所以把官方的半透明层**等价换算成实色**，视觉接近、实现简单得多：

| token | 值 | 对应官方哪一层 | 用途 |
|---|---|---|---|
| `--bg-page` | `#12212f` | `#0D2439` 再压 1 档 | body 兜底、页脚 |
| `--bg-panel` | `#1b2b3a` | 白 6% 叠在 #0D2439 上 | 侧栏卡片 |
| `--bg-inset` | `#0d1a26` | 黑 25% 叠在 #0D2439 上 | 规格表斑马纹、chip |
| `--line` | `rgba(255,255,255,0.05)` | 官方原值 | 卡片描边 |
| `--line-strong` | `rgba(255,255,255,0.20)` | 官方主面板描边 | 控件描边、hover |
| `--text-primary` | `#ffffff` | 官方原值 | 标题 |
| `--text-strong` | `#dddddd` | 官方正文色，兼作值色 | 正文、键值对的值 |
| `--text-muted` | `#a6b2c0` | `rgba(255,255,255,0.7)` 的等价值 | 小标签、次级说明 |

> [计算] 换算基于 sRGB 直接混合，未做线性化。上线后用取色工具在实际页面上比一次。

### 3.2 主题层（CS2）[实测]

```css
--game-bg:           #0d2439;
--game-accent:       #2c74ce;
--game-accent-text:  #5097f4;
--game-accent-soft:  rgb(44 116 206 / 40%);
```

来源：官方站 `:root` 上运行时注入的 `--gamePage*` 四个变量。

**用法**：整页底色用 `--game-bg`——这就是官方详情页的真实底色，见 `GamePage-styles__root { background: var(--gamePageBackgroundColor) }`。`--bg-page` 只作 body 兜底和页脚。

```css
body { background: var(--bg-page); }
.page { background: var(--game-bg); min-height: 100vh; }
```

### 3.3 状态色

| 语义 | 文字/描边色 | 填充色（白字） | 本站用途 |
|---|---|---|---|
| 成功 | `#5abe41` | `#2d9640` | 验证项"已完成"、`稳定` 状态 |
| 警示 | `#f2a83b` | — | `预览` 状态 |
| 信息 | `#5097f4` | `#2c74ce` | `测试` 状态、链接、主按钮 |
| 错误 | `#ef2f2f` | — | 表单/加载错误，本站基本不出现 |

`#2d9640` 不是自定值，是官方主按钮的描边色（`.src-components-InstallModButton-styles__button` 的 `border: 2px solid #2d9640` [实测]）。

### 3.4 对比度核对 [计算]

| 组合 | 比值 | 结论 |
|---|---|---|
| `#dddddd` 正文 / `#12212f` | ≈ 11:1 | 通过 |
| `#a6b2c0` 小标签 / `#12212f` | ≈ 7.4:1 | 通过 |
| `#a6b2c0` 小标签 / `#1b2b3a` | ≈ 6.3:1 | 通过 |
| 白字 / `#2c74ce` | ≈ 4.7:1 | 通过，余量小：字号 ≥14px、字重 ≥600 |
| 白字 / `#2d9640` | ≈ 4.4:1 | 临界，主按钮字号不要小于 15px |
| 白字 / `#5abe41` | ≈ 2.4:1 | **不通过**，`#5abe41` 只做文字和描边 |
| `#5097f4` 链接 / `#12212f` | ≈ 6.6:1 | 通过 |

> v1 那张表按 `#191d25` 算。底色改为 `#0d2439` 系后数值已重算，但仍是手算，上线前用 axe 或 Lighthouse 复核。

### 3.5 为什么不复制毛玻璃 [自定]

官方的 `backdrop-filter: blur(16px) contrast(0.75) brightness(0.5)` 是**为了压住身后的游戏键图**。本站背后是纯色，同样的模糊等于什么都没做，只会多一层 GPU 合成开销和一处 Safari 兼容风险。

所以：**保留它的层次结构（面板 / 卡片 / 斑马纹三层叠加），换成实色。**
唯一保留的阴影是侧栏卡片那圈 `0 0 4px 6px rgba(0,0,0,0.03)` 的"空气感"——它太淡了，换成描边更干净，本站直接省略。

---

## 4. 字体与排版

### 4.1 字体族 [实测]

```css
--font: "Source Sans 3", "Source Sans Pro", "Noto Sans SC",
        "PingFang SC", "Microsoft YaHei", system-ui, sans-serif;
```

- 官方站全站只有一个字体族 `SourceSansPro, sans-serif`，没有第二字体。
- Source Sans 没有中文字形，中文必须回退。
- **自托管字体，不依赖 Google Fonts。** 拉丁部分自托管 woff2，`font-display: swap`；中文走系统字体。[自定]

### 4.2 字号层级 [实测]

官方站描述区是富文本，所以下面这组是**描述区内的标题层级**，不是页面 H1/H2：

| 层级 | 字号 | 其他 | 来源 |
|---|---|---|---|
| 描述区 h1 | 32px | `padding-top: 20px`，`#fff` | `.DescriptionSection-styles__description h1` |
| 描述区 h2 | 26px | `padding-top: 14px`，`#eee` | 同上 `h2` |
| 描述区 h3 | **22px** | `padding-top: 8px` | 同上 `h3` |
| 描述区 h4/h5/h6 | **20px** | — | 同上 |
| 正文 | **16px** | `color: #ddd`，`margin-bottom: 24px` | `.DescriptionSection-styles__description` |
| mod 标题 | **32px / 36px / bold** | `margin-top: 8px` | `.Thumbnail-styles__displayName` |
| 键值对的值 | **18px / 400** | 左对齐，`flex: 1` | `.Metadata-styles__value` |
| 小标签 | **12px / 300** | `letter-spacing: 2px`，`rgba(255,255,255,.7)` | 见 4.3 |

**v1 的错**：把 h3 写成 18px、正文写成 15px、值写成 14px/600 右对齐。实测分别是 22px、16px、18px/400 **左对齐**。

> 移动端官方把标签压到 10px、值压到 14px/600（`.device-mobile` 变体）。本站最小字号定 12px，不跟这条。

### 4.3 小标签（eyebrow）—— 全站最强的识别特征

官方**两个组件用同一套**（`DescriptionSection-styles__subTitle` 与 `SideCard-styles__title`，声明逐字相同）[实测]：

```css
font-family: SourceSansPro, sans-serif;
font-size: 12px;
font-weight: 300;          /* 细体，不是粗体 */
letter-spacing: 2px;       /* 绝对值，不是 em */
text-transform: uppercase;
color: rgba(255, 255, 255, 0.7);
margin-bottom: 16px;
height: 12px;
```

规格表的键是同一套的**加粗变体**（`.Metadata-styles__label`）[实测]：

```css
font-size: 12px;
font-weight: 600;
letter-spacing: 1px;       /* 比区块头的 2px 紧 */
color: rgba(255, 255, 255, 0.75);
width: 40%;
margin-right: 16px;
padding-top: 2px;
line-height: 14px;
```

**v1 的错**：写成 12px / 700 / `--text-muted` / `0.08em`。实测是 **300（区块头）与 600（规格表键）两档**，字距 2px 与 1px，颜色是白 70%/75%。

本站规则 [自定]：

1. **只用于结构**：规格表的键、右栏区块头、状态表头。不要在每个 H2 上方再叠一个。
2. **区块头用 300，规格表键用 600**，两档并存，别统一。
3. **中文版**：中文没有大小写，`text-transform` 无效，改为 `letter-spacing: 0.18em`、字重 500。不要硬把英文大写塞进中文页。

---

## 5. 栅格、布局与响应式

### 5.1 官方站的真实骨架 [实测]

它不是固定栅格，是 **flex**：

```css
/* GamePage-styles__root */
background: var(--gamePageBackgroundColor);
padding: 0 10px;

/* GamePage-styles__content */
max-width: var(--width-inner-container);   /* 1202px */
margin: 0 auto;
padding: 30px 0 60px 0;

/* Mods-Details-styles__root */
display: flex; padding: 16px; gap: 32px;

/* mainContent */
flex: 1; min-width: 0;

/* metadata（右栏） */
width: 360px;      /* 桌面 */
width: 300px;      /* 中屏 */
width: 280px;      /* 小屏 */
width: auto;       /* 移动端 */
```

换算：`1202 - 16*2 - 32 - 360 = 778`。所以桌面是 **778 : 360**，不是 v1 的 740 : 300。

### 5.2 本站 token [自定]

```css
--container:   1202px;   /* 外层，[实测] */
--col-side:    360px;    /* 右栏，[实测] */
--col-gap:     32px;     /* [实测] */
--pad-panel:   16px;     /* [实测] */
/* 主栏 = flex:1，约 778px */
```

用 flex 或 grid 都行，但**右栏固定 360、主栏自适应**这个关系要保住。

### 5.3 页面骨架

```
┌──────────────────────────────────────────────────────────┐
│ Header  品牌 + 面包屑      区块锚点   语言切换             │ 56px
├──────────────────────────────────────────────────────────┤
│ ┌───────────────────────────────┐  ┌───────────────────┐ │
│ │ 主媒体 16:9，圆角 8px           │  │ H1 mod 标题  32px  │ │
│ │（无截图时：示意图，见 6.9）      │  │ 作者 + 状态标签     │ │
│ ├───────────────────────────────┤  │ ┌───────────────┐ │ │
│ │ 缩略图条（≥2 张媒体才显示）      │  │ │ 唯一主按钮     │ │ │
│ └───────────────────────────────┘  │ └───────────────┘ │ │
│                                    │ 规格表（斑马纹）    │ │
│ 简介                                │ TAGS              │ │
│ 它做了什么                           │ LINKS             │ │
│ 设置                                │ CHANGELOG ▾       │ │
│ 验证状态                             │                   │ │
│ 安装                                │                   │ │
│ 已知限制                             │                   │ │
│ 常见问题                             │                   │ │
│ └─ flex:1 ≈778 ─────────────────┘   └─ 360 ────────────┘ │
├──────────────────────────────────────────────────────────┤
│ Footer：非官方声明 · 仓库 · 作者 · 语言                      │
└──────────────────────────────────────────────────────────┘
```

官方是"媒体左、元信息右"，本站沿用。**区别**：本站暂时没有真实截图，首屏左侧是示意图，所以右栏的状态标签和主按钮必须在首屏可见。

### 5.4 响应式断点 [自定]

官方有四档右栏宽度（360 / 300 / 280 / auto），具体断点值未逐一核实。本站简化为三档：

| 视口 | 行为 |
|---|---|
| ≥ 1024px | 778 : 360 双栏 |
| 640–1023px | 右栏降到 300，主栏自适应 |
| < 640px | 单栏。顺序：H1 与状态 → 主媒体 → 主按钮 → 规格表 → 正文各区块 → 右栏其余；容器左右留白 16px |

单栏下规格表要早于正文出现，因为它回答"能不能用"。

### 5.5 间距与形状 [实测]

| 项 | 官方值 | 本站 |
|---|---|---|
| 详情面板 padding | `16px` | 同 |
| 栏间距 | `32px` | 同 |
| 侧栏卡片 padding | `16px`，`margin-top: 24px` | 同 |
| 规格表行 padding | `8px 12px` | 同 |
| 面板圆角 | **`8px`** | 同 |
| 卡片/控件圆角 | **`4px`** | 同 |
| 主按钮圆角 | **`24px`**（胶囊化） | 同 |
| 标签 chip 圆角 | `4px`，`border: 2px solid transparent` | 同 |
| 描边 | `1px`，主面板 `rgba(255,255,255,.2)` | 同 |
| 阴影 | 面板有、卡片极淡 | **面板用描边，卡片不用**（见 3.5） |

**v1 的错**：卡片圆角写成 18px、主按钮圆角写成 4px。实测是 **8px / 24px**。18px 是我上一版从**列表页卡片**目测的，误用到了详情页。

---

## 6. 组件

### 6.1 Header
高 56px，深色条。左：品牌小图标 + 站名 + `›` + 当前 mod 名（面包屑）。右：区块锚点、语言切换。不做吸顶，单页锚点跳转即可。[自定]

### 6.2 按钮

官方主按钮 [实测] `.src-components-InstallModButton-styles__button`：

```css
min-height: 48px;
border-radius: 24px;
border: 2px solid #2d9640;
padding: 8px 16px;
font-family: SourceSansPro, sans-serif;
display: flex; align-items: center; justify-content: space-between;
```

三件事要记住：**胶囊圆角 24px**、**2px 描边**、**48px 高**。不是 4px 圆角的小方块。

| 类型 | 规格 | 用途 |
|---|---|---|
| 主按钮 | 高 48px，圆角 24px，`--game-accent` 底 + 2px 同色描边，白字 600 | **全页唯一**一个 |
| 描边按钮 | 高 40px，圆角 4px，1px `--line-strong`，透明底，白字 600 | 次要操作 |
| 文字按钮 | 无边框，`--game-accent-text` | 行内链接式操作 |
| 图标按钮 | 32–40px，圆角 8px，半透明底 + 1px 描边 | 复制、展开 |

主按钮含义随发布状态切换 [自定]，由 `mod.json` 的 `distribution` 决定：

| 发布状态 | 主按钮 | 次要 |
|---|---|---|
| 仅有 GitHub | 查看源码 / View on GitHub | 无 |
| 已上架 Paradox Mods | 在 Paradox Mods 订阅 / Subscribe | GitHub 降级为描边 |

### 6.3 规格表（右栏）—— 官方最有价值的组件

官方实现 [实测] `.src-components-Mods-Details-Metadata-styles__*`：

```css
/* item 行 */
display: flex; flex-direction: row; align-items: center;
padding: 8px 12px; border-radius: 4px;

/* 斑马纹：奇数行 */
background-color: rgba(0, 0, 0, 0.25);

/* label 键 */
width: 40%; margin-right: 16px;
font-size: 12px; font-weight: 600; letter-spacing: 1px;
text-transform: uppercase; line-height: 14px; padding-top: 2px;
color: rgba(255, 255, 255, 0.75);

/* value 值 */
flex: 1; font-size: 18px; font-weight: 400;   /* 左对齐，不是右对齐 */
```

**三个和 v1 不同的点**：

1. 分隔靠**斑马纹**（`rgba(0,0,0,.25)`），不是 1px 横线。
2. 值**左对齐**，18px/400，比键大得多。键小、值大，这是它的信息层级逻辑。
3. 行本身有 4px 圆角，斑马纹整行铺色，不是下划线。

用语义化 `<dl>`，不要 `<table>`。

**本站字段** [自定]，缺数据整行隐藏，**不显示 0、"—" 或占位数字**：

| 键 | 值来源 |
|---|---|
| 状态 STATUS | `mod.json.status`，渲染为状态标签（6.4） |
| 版本 VERSION | `mod.json.version` |
| 游戏版本 GAME | 兼容的 CS2 版本，待填 |
| 类型 TYPE | `Code Mod` |
| 平台 PLATFORM | PC |
| 作者 AUTHOR | Nanako-Arasaka |
| 更新 LAST UPDATED | `mod.json.updated` |
| 许可 LICENSE | 有声明才显示 |

**不放**：订阅数、点赞数、体积、协作者。没有真实数据，也不应编造。

### 6.4 状态标签（status chip）[自定]

全站唯一带颜色的标签，表达 mod 成熟度：

| 状态值 | 中文 | 英文 | 颜色 |
|---|---|---|---|
| `preview` | 预览版 | Preview | `--warn` `#f2a83b` |
| `beta` | 测试版 | Beta | `--game-accent-text` `#5097f4` |
| `stable` | 稳定版 | Stable | `--ok` `#5abe41` |

样式：1px 同色描边 + 同色文字，无填充，圆角 999px，12px/600。右侧可跟一句限定语，例如"预览版 · 尚未完成游戏内验收"。

### 6.5 普通标签 chip [实测]

官方 `.src-components-Mods-Details-Tags-styles__tag`：

```css
display: inline-block;
background-color: rgba(0, 0, 0, 0.3);      /* 有底色，不是透明 */
padding: 4px 8px;
margin: 0 8px 8px 0;
border-radius: 4px;
border: 2px solid transparent;             /* 平时透明，hover 才显色 */
transition: color .3s, background-color .3s, border-color .3s;
/* hover: background var(--gamePageActiveColorTransparent); border-color var(--gamePageActiveColor) */
```

本站照抄结构，但**不上色**：普通标签保持中性底 + 中性边，有颜色的只有 6.4 的状态标签。

### 6.6 设置表
把 mod 的 4 个设置项展示给玩家。每行：设置名 / 默认值 / 范围 / 一句话说明。样式与规格表一致，小屏下说明折到第二行。内容见第 7 节。

### 6.7 验证清单

```
✓  Release 构建：0 警告 0 错误          已完成
✓  倍率换算回归：167,670 项断言通过       已完成
○  线路面板滑块实际可拖到新上限            未验证
○  7 类交通逐项出车                       未验证
```

- 已完成：`--ok` 对勾图标，文字 `--text-strong`。
- 未验证：**中性**空心圆，文字 `--text-muted`。未验证不等于有问题，不用红或警示色。
- 底部固定一行"截至 {date}"，来自 `mod.json.validation.asOf`。

### 6.8 折叠列表（CHANGELOG、FAQ）
用原生 `<details>`。每行：版本号或问题（白，600）+ 日期（灰）+ 展开箭头。底部居中描边按钮 `SHOW ALL`，仅当条目超过 5 条时出现。

### 6.9 示意图（无截图时的主媒体）[自定]
16:9、圆角 **8px**、1px 描边、`--bg-panel` 底。内容是一张线框 SVG：同一个滑块，上方是原版上限，下方是扩展后的上限（默认 ×5）。要求：

1. 右下角固定小标签"示意图 / Illustration"，不能让人误以为游戏截图。
2. 只用主题色和灰阶，不画车辆、不仿制游戏 UI。
3. 有真实截图后，示意图移到"它做了什么"一节，首屏换截图。

官方缩略图另有 `box-shadow: 0 0 8px 4px var(--gamePageBackgroundColorTransparent)` 的光晕 [实测]，本站不复制（无键图背景，见 3.5）。

### 6.10 图标
统一线框风格，描边 1.5px，不用彩色 logo 原色。圆形容器约 40px，下方小字标签。

---

## 7. 页面区块与内容来源

下列内容全部来自仓库 README，**[README]**，网站不得补充 README 里没有的功能或效果。

| # | 区块 | 内容 |
|---|---|---|
| 1 | 简介 | 一句话：把原版线路面板里"配车数量"滑块的上限放大。覆盖公交、有轨电车、地铁、火车、船、轮渡、飞机共 7 类交通，每条线路仍单独调节 |
| 2 | 它做了什么 | 示意图 + 三条要点：全局选项只决定滑块上限，不会替玩家把现有配车数乘倍数；不依赖 Harmony、不替换原版策略系统、不每帧写数据；卸载时会还原 |
| 3 | 设置 | 见下表 |
| 4 | 验证状态 | 6.7 清单，数据来自 `mod.json.validation` |
| 5 | 安装 | 随发布状态切换，见 8.4 |
| 6 | 已知限制 | 见下方列表 |
| 7 | 常见问题 | `<details>` 折叠，内容由作者后续补充 |

**设置表** [README]

| 设置项 | 默认 | 范围 |
|---|---|---|
| 解锁出车数量上限 | 开 | 开 / 关 |
| 线路出车上限倍率 | 5 | 1–10 |
| 同步放大车库容量 | 开 | 开 / 关 |
| 车库容量倍率 | 5 | 1–10 |

车库放大不包含出租车。

**已知限制（README 原意，用玩家语言改写）**

- 提高可用车辆数不代表立刻有车出发，实际出车仍取决于原版的调度。
- 不支持"绝对值模式"：遇到这种策略会保持初始范围并给出警告。
- 倍率是相对 mod 首次读到的原版数值计算的。如果别的 mod 更早改过同一份数据，无法保证那是原版值。

> 技术细节（ECS 生命周期、换算公式、构建脚本）不放进首屏和主要区块，放到"面向开发者"折叠区，附仓库链接。

---

## 8. 内容与文案规则：状态诚实

这是本站和官方站最大的区别。官方描述的是成熟平台，本站要介绍的是一个**还没完成游戏内验收**的 mod。[README] 明确写着：滑块实际表现、7 类交通逐项效果、存档往返、跨城市加载、卸载还原都尚未实测，也还没有发布流程和 Releases。

### 8.1 单一事实来源
状态、版本、验证项、发布渠道全部来自 `mod.json`（第 12 节）。页面文案不得手写状态，避免"文案说稳定、数据说预览"。

### 8.2 状态晋升规则 [自定]
- `stable` 只有在 `validation` 里**所有条目都是 done** 时才允许使用。
- 在此之前，页面不得出现"稳定""完美""零问题""开箱即用"这类词，也不得用"已测试"笼统概括。
- 写验证状态时，必须同时写**已验证什么、未验证什么**，并带日期。

### 8.3 语气
- 说明书口吻，主动语态，不用营销形容词。
- 按钮说清楚发生什么："查看源码""在 Paradox Mods 订阅"，不用"立即体验""了解更多"。
- 同一个动作全站叫同一个名字。

### 8.4 安装区随状态切换 [自定]

| 状态 | 显示 |
|---|---|
| 仅有源码、无 Release | "目前仅提供源码构建，尚无可直接安装的发布版"，附仓库链接与构建说明链接 |
| 已上架 Paradox Mods | "在 Paradox Mods 订阅，游戏内自动安装"，附官方页链接 |

只有存在真实的发布版时，才写"下载 / 订阅 / 安装"这类动作。

### 8.5 非官方声明与素材
- 页脚固定一行：「本站为社区作品，与 Paradox Interactive、Colossal Order 无隶属关系。」英文：*Unofficial community project. Not affiliated with or endorsed by Paradox Interactive or Colossal Order.*
- **不使用**官方 Logo、CS2 宣传图、Paradox Mods 的图标和品牌字标。游戏名只以文字出现。
- 参考的是官方站的**排版和组件调性**，不是它的素材。

---

## 9. 动效与交互

沿用官方站"功能性、克制"的调性。官方实际用的时长 [实测]：hover 描边 `0.2s`，标签 chip `0.3s`，缩略图展开按钮 `0.3s`（带 `0.1s` 延迟）。

| 场景 | 时长 | 曲线 |
|---|---|---|
| hover 描边/颜色 | 120ms | `--ease-out` |
| `<details>` 展开 | 200ms | `--ease-out` |
| 锚点滚动 | 浏览器默认，`scroll-behavior: smooth` | — |

```css
--ease-out: cubic-bezier(0.22, 1, 0.36, 1);
--dur-fast: 120ms;
--dur-base: 200ms;
```

规则：
1. **没有滚动显现，没有页面载入的入场动画。** 动效只回应用户操作。
2. 卡片 hover 只变描边，不位移、不加阴影。
3. 支持 `prefers-reduced-motion: reduce`：时长归零，`scroll-behavior: auto`。
4. 图片懒加载期间显示低分辨率模糊占位，不显示骨架块。

---

## 10. 无障碍

- **对比度**：以 3.4 为准，小标签一律用 `--text-muted`。
- **焦点环**：`outline: 2px solid var(--game-accent-text); outline-offset: 2px`。这是全站唯一允许的"外发光"。
- **点击区域**：触屏下不小于 44×44px（视觉可以更小，用 padding 扩）。官方主按钮 48px 高，正好达标。
- **语义**：全页一个 `<h1>`；规格表用 `<dl>`；验证清单用 `<ul>`，图标补 `aria-label`，不要只靠形状表达状态。
- **状态不只靠颜色**：状态标签和验证项都必须有文字。
- **示意图**：SVG 带 `role="img"` 与 `aria-label`，并写明这是示意图。
- 装饰性图标 `aria-hidden="true"`。

---

## 11. 双语

- 默认中文，提供英文，所有可见文案走同一份文案表（`copy.zh` / `copy.en`），**不在组件里写死字符串**。
- 两种语言必须**结构对等**：同样的区块、验证项、状态。不允许某一语言多出"功能"或少了"限制"。
- 页面 `<html lang>` 随语言切换。
- 英文标题（`displayName.en`）目前未定，见第 17 节。

---

## 12. 数据文件 `mod.json`

站点内容的唯一数据源。字段示例（值为占位或 README 事实，**不是**最终内容）：

```json
{
  "name": "VehicleCapUnlock",
  "displayName": { "zh": "<待定>", "en": "<待定>" },
  "version": "<从 mod 元数据读取>",
  "status": "preview",
  "updated": "<YYYY-MM-DD>",
  "distribution": {
    "paradoxMods": null,
    "github": "https://github.com/Nanako-Arasaka/CS2_Unlimited_Vehicle_Better_Death"
  },
  "supports": ["bus", "tram", "subway", "train", "ship", "ferry", "airplane"],
  "garageExcludes": ["taxi"],
  "settings": [
    { "key": "unlockCap",       "default": true, "range": "on/off" },
    { "key": "lineCapMultiple", "default": 5,    "range": [1, 10] },
    { "key": "scaleGarage",     "default": true, "range": "on/off" },
    { "key": "garageMultiple",  "default": 5,    "range": [1, 10] }
  ],
  "validation": {
    "asOf": "2026-11-17",
    "items": [
      { "id": "build",    "state": "done",    "text": "Release 构建 0 警告 0 错误" },
      { "id": "math",     "state": "done",    "text": "倍率换算回归 167,670 项断言通过" },
      { "id": "slider",   "state": "pending", "text": "线路面板滑块可拖到新上限" },
      { "id": "dispatch", "state": "pending", "text": "7 类交通逐项出车" },
      { "id": "save",     "state": "pending", "text": "存档往返与跨城市加载" },
      { "id": "restore",  "state": "pending", "text": "卸载后还原" }
    ]
  }
}
```

`validation.items` 的状态只来自仓库里真实的验证记录；每次游戏内验证后更新 `state` 和 `asOf`，其余不动。

---

## 13. 与两份参考的取舍

| 方面 | Paradox Mods（本站靠拢） | StressWatch（本站取舍） |
|---|---|---|
| 底色 | 深色 + 主题氛围色，面板靠半透明叠加 | 不继承明暗 tile 交替 |
| 强调色 | CS2 蓝 `#2c74ce` | 不继承 Apple Action Blue |
| 圆角 | 4 / 8 / 24 / 999 | 不继承大圆角玻璃卡片 |
| 阴影 | 面板有、卡片极淡；换算成实色时省掉 | 不继承软阴影 |
| 版式 | flex 双栏 778:360、斑马纹规格表 | 不继承居中大标题式首屏 |
| 动效 | 快、功能性 | 不继承 700–900ms 长曲线 |
| 字重 | 标题 bold，小标签 300/600 双档 | 不继承"字重不超过 600" |
| 字体 | 单一族 | 同样单一族，且都含中文回退 |
| **继承的工程习惯** | — | token 化、禁止裸色值、文案表双语对等、`prefers-reduced-motion`、提交前检查清单、"状态/免责声明靠数据驱动" |

一句话：**视觉靠 Paradox Mods，工程纪律靠 StressWatch。**

---

## 14. 不要做的事

1. 不要再引入第二个主按钮。
2. 不要用彩色分类标签，除了 6.4 的状态标签。
3. 不要加毛玻璃、彩色光晕、渐变装饰。官方主面板的毛玻璃是为压住键图，本站没有键图（见 3.5）。
4. 不要在每个 H2 上方叠大写小标签。
5. 不要显示没有真实来源的数字（订阅数、下载量、评分）。
6. 不要用假的游戏截图，或拿官方宣传图当主图。
7. 不要写"稳定""完美""已完全测试"，除非 `validation` 全部 done。
8. 不要把 Cookie 弹窗等第三方组件的样式当参考。
9. 不要把列表页首行的模糊图当设计意图，那是懒加载占位。
10. 不要在组件里写死中文或英文字符串。
11. **不要把规格表的值右对齐。** 官方是左对齐、18px、比键大。
12. **不要把区块头小标签做成粗体。** 官方是 300 细体。

---

## 15. CSS 骨架

```css
:root {
  /* 框架层（半透明层已换算为实色，见 3.1） */
  --bg-page:       #12212f;
  --bg-panel:      #1b2b3a;
  --bg-inset:      #0d1a26;
  --line:          rgba(255, 255, 255, 0.05);
  --line-strong:   rgba(255, 255, 255, 0.20);
  --text-primary:  #ffffff;
  --text-strong:   #dddddd;
  --text-body:     #dddddd;
  --text-muted:    #a6b2c0;

  /* 主题层（只改这 4 个） */
  --game-bg:           #0d2439;
  --game-accent:       #2c74ce;
  --game-accent-text:  #5097f4;
  --game-accent-soft:  rgb(44 116 206 / 40%);

  /* 状态 */
  --ok:        #5abe41;
  --ok-fill:   #2d9640;
  --warn:      #f2a83b;
  --error:     #ef2f2f;

  /* 形状与宽度 */
  --r-panel:   8px;
  --r-control: 4px;
  --r-btn:     24px;
  --r-pill:    999px;
  --container: 1202px;
  --col-side:  360px;
  --col-gap:   32px;
  --pad-panel: 16px;

  /* 动效 */
  --ease-out: cubic-bezier(0.22, 1, 0.36, 1);
  --dur-fast: 120ms;
  --dur-base: 200ms;

  --font: "Source Sans 3", "Source Sans Pro", "Noto Sans SC",
          "PingFang SC", "Microsoft YaHei", system-ui, sans-serif;
}

body { background: var(--bg-page); color: var(--text-body);
       font: 400 16px/1.6 var(--font); }
.page { background: var(--game-bg); }
.detail { max-width: var(--container); margin: 0 auto;
          display: flex; gap: var(--col-gap); padding: var(--pad-panel); }
.detail__main { flex: 1; min-width: 0; }
.detail__side { width: var(--col-side); flex: none; }

/* 区块头小标签：细体 */
.eyebrow {
  font-size: 12px; font-weight: 300; line-height: 12px;
  letter-spacing: 2px; text-transform: uppercase;
  color: var(--text-muted); margin-bottom: 16px;
}
:lang(zh) .eyebrow { letter-spacing: 0.18em; font-weight: 500; text-transform: none; }

/* 规格表：斑马纹 + 值左对齐 18px */
.spec { display: flex; flex-direction: column; }
.spec__row { display: flex; align-items: center; gap: 16px;
             padding: 8px 12px; border-radius: 4px; }
.spec__row:nth-child(2n+1) { background: var(--bg-inset); }
.spec__key { width: 40%; flex: none;
             font-size: 12px; font-weight: 600; line-height: 14px;
             letter-spacing: 1px; text-transform: uppercase; padding-top: 2px;
             color: var(--text-muted); }
.spec__val { flex: 1; font-size: 18px; font-weight: 400;
             font-variant-numeric: tabular-nums; }

.btn { display: inline-flex; align-items: center; justify-content: center;
       font: 600 15px/1 var(--font); border: 0; cursor: pointer; }
.btn--primary { min-height: 48px; padding: 8px 20px; border-radius: var(--r-btn);
                background: var(--game-accent); color: #fff;
                border: 2px solid var(--game-accent); }
.btn--outline { height: 40px; padding: 0 16px; border-radius: var(--r-control);
                background: transparent; color: #fff;
                border: 1px solid var(--line-strong); }

.chip { background: var(--bg-inset); border-radius: var(--r-control);
        padding: 4px 8px; border: 2px solid transparent;
        font-size: 13px; color: var(--text-strong); }
.status { border: 1px solid currentColor; border-radius: var(--r-pill);
          padding: 2px 10px; font: 600 12px/16px var(--font); }
.status--preview { color: var(--warn); }
.status--beta    { color: var(--game-accent-text); }
.status--stable  { color: var(--ok); }

:focus-visible { outline: 2px solid var(--game-accent-text); outline-offset: 2px; }

@media (prefers-reduced-motion: reduce) {
  * { transition-duration: 0ms !important; scroll-behavior: auto !important; }
}
```

---

## 16. 提交前检查清单

- [ ] 页面里只有一个主按钮。
- [ ] 没有裸色值（`#xxxxxx`），颜色都走 token。
- [ ] 没有 `box-shadow`（焦点环用 `outline`；侧栏卡片用描边替代）。
- [ ] 区块头小标签是 **300 细体 + 2px 字距**；规格表键是 **600 + 1px 字距**。
- [ ] 规格表的值是**左对齐 18px/400**，斑马纹用 `--bg-inset`，不是 1px 横线。
- [ ] 主按钮是 24px 圆角、48px 高、2px 描边。
- [ ] 规格表缺数据的行已隐藏，没有 0 或 "—"。
- [ ] 状态文案与 `mod.json.status` 一致；有"稳定"字样时，验证项全部 done。
- [ ] 验证清单带日期，已完成与未验证都有文字说明。
- [ ] 主按钮文案符合当前发布状态（8.4）。
- [ ] 没有用官方素材，页脚有非官方声明。
- [ ] 中英文区块、验证项、限制说明一一对应。
- [ ] 键盘可操作，焦点环可见；开启"减弱动态效果"后无动画。
- [ ] 在 375px 宽度下无横向滚动。

---

## 17. 待确认事项

**需要你决定**

1. **正式名称**：面向玩家的中文名和英文名？仓库名 `Pubilc` 是 `Public` 的拼写错误，是否发布前改掉？
2. **有没有游戏内截图或 GIF？** 没有则首屏用 6.9 的示意图。
3. **何时上架 Paradox Mods？** 这决定主按钮和安装区内容。
4. **许可证**：是否声明？声明了才在规格表显示。
5. **技术栈与托管**：沿用 StressWatch 站的栈，还是纯静态页？本文 token 与框架无关。
6. **是否需要独立的更新日志页**，还是只用右栏折叠列表。

**官方站核对：v1 的 6 项已全部结案**

| v1 问题 | 结论 | 证据 |
|---|---|---|
| 详情页整页背景 `#191d25` 还是 `#0d2439` | **`#0d2439`**。`#191d25` 只是 body 兜底 | `GamePage-styles__root { background: var(--gamePageBackgroundColor) }` |
| 面板底色 `#1f242d` 还是 `#2c2c2c` | **都不是**，是 `rgba(255,255,255,.06)` 等半透明叠加 | `SideCard-styles__root`、`Metadata-styles__item` |
| 1202px 容器与 740:300 比例 | 容器 1202 ✓；比例实为 **778:360**（flex，右栏 360 固定） | `Mods-Details-styles__metadata { width: 360px }` |
| CS2 主题 4 个变量 | 全部确认 | `:root` 运行时注入 |
| 字体是否 Source Sans Pro | 确认，且全站唯一 | `font-family: SourceSansPro, sans-serif` |
| H1/H2/正文真实字号 | h1 32 / h2 26 / h3 **22** / h4-6 20 / 正文 **16** | `DescriptionSection-styles__description` |

**新增待确认（要你判断，不是核对）**

7. 右栏 360px 在 1170 面板里占 30%，本站内容是单 mod、信息量少，是否收到 320px？
8. 官方正文 16px 在中文下偏大，是否降到 15px？（v1 建议 15px，实测官方 16px）

---

## 18. 对 v1 的修订记录

| v1 说法 | 实测 | 处理 |
|---|---|---|
| 详情页底色待定，倾向 `#191d25` | `#0d2439` | 3.2 改写，body 兜底与页面底色分开 |
| 面板 `#1f242d` | `rgba(255,255,255,.06)` 叠加层 | 3.0 新增"官方真实做法"，3.1 给实色换算 |
| "全站无阴影，分层靠描边" | 面板有 `0 4px 8px 2px rgba(0,0,0,.2)`，卡片有极淡阴影，并有 `backdrop-filter` | 原则 4 改写；3.5 说明本站为何换实色 |
| 740 : 300 双栏 | **778 : 360**（flex:1 + 固定 360） | 5.1 / 5.2 重写 |
| 卡片圆角 18px | 面板 8px、卡片 4px、按钮 24px | 5.5 重写；6.2 补按钮规格 |
| 主按钮 4px 圆角 | **24px 圆角、48px 高、2px 描边 `#2d9640`** | 6.2 重写 |
| 小标签 12px / 700 / `#697582` / 0.08em | **12px / 300（区块头）· 600（规格表键）/ 白 70%–75% / 2px · 1px** | 4.3 重写，两档并存 |
| 正文 14px（v1 改 15px） | **16px** | 4.2 改为 16，中文是否降 15 列入待确认 |
| h3 = 18px | **22px** | 4.2 修正；h4-6 = 20px 补充 |
| 规格表"值右对齐 14/600 + 1px 分隔线" | **左对齐 18/400 + 斑马纹 `rgba(0,0,0,.25)`** | 6.3 重写 |
| chip"透明底 + 1px 描边" | **`rgba(0,0,0,.3)` 底 + 2px 透明描边，hover 显色** | 6.5 重写 |
| "不要加阴影、玻璃模糊" | 官方主面板就是毛玻璃 + 阴影 | 14.3 改为"本站不用毛玻璃"，并给理由 |
| 对比度表按 `#191d25` 算 | 底色改为 `#0d2439` 系 | 3.4 全部重算 |
| 文中笔误 `glype` | — | 已改为"图标" |

**v2 新增**：3.0（官方真实分层做法）、3.5（毛玻璃的取舍理由）、6.2 官方按钮完整规格、17 节结案表、14 节两条新禁令（值右对齐、区块头粗体）。
