// Construye la guía: une los capítulos Markdown listados en ../guia.json,
// los convierte a HTML (con resaltado de código y diagramas Mermaid) y genera el PDF
// usando Chrome o Edge en modo headless.
//
// Uso (desde esta carpeta):  npm install   (solo la primera vez)
//                            npm run construir
//
// Salida: ../salida/guia.html y ../salida/<archivoSalida>.pdf

import { Marked } from 'marked';
import { markedHighlight } from 'marked-highlight';
import hljs from 'highlight.js';
import puppeteer from 'puppeteer-core';
import { fileURLToPath, pathToFileURL } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';

const aqui = path.dirname(fileURLToPath(import.meta.url));
const raizGuia = path.resolve(aqui, '..');
const config = JSON.parse(fs.readFileSync(path.join(raizGuia, 'guia.json'), 'utf8'));
const carpetaSalida = path.join(raizGuia, 'salida');
fs.mkdirSync(carpetaSalida, { recursive: true });

// ---------- Markdown -> HTML ----------

// Algunos lenguajes no existen en highlight.js con ese nombre: los traducimos.
const aliasLenguajes = { cshtml: 'xml', razor: 'xml', text: 'plaintext', txt: 'plaintext', http: 'http', ps1: 'powershell' };

const marked = new Marked(
  markedHighlight({
    emptyLangClass: 'hljs',
    langPrefix: 'hljs language-',
    highlight(codigo, lenguaje) {
      if (lenguaje === 'mermaid') return codigo;
      const real = aliasLenguajes[lenguaje] ?? lenguaje;
      return hljs.getLanguage(real)
        ? hljs.highlight(codigo, { language: real }).value
        : hljs.highlight(codigo, { language: 'plaintext' }).value;
    }
  })
);

const indice = [];
const idsUsados = new Set();

function crearId(texto) {
  let base = texto.toLowerCase()
    .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
    .replace(/<[^>]+>/g, '')
    .replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'seccion';
  let id = base, n = 2;
  while (idsUsados.has(id)) id = `${base}-${n++}`;
  idsUsados.add(id);
  return id;
}

marked.use({
  renderer: {
    heading({ tokens, depth }) {
      const html = this.parser.parseInline(tokens);
      const texto = html.replace(/<[^>]+>/g, '');
      const id = crearId(texto);
      if (depth <= 2) indice.push({ nivel: depth, texto, id });
      return `<h${depth} id="${id}">${html}</h${depth}>\n`;
    },
    code({ text, lang }) {
      if (lang === 'mermaid') {
        return `<figure class="diagrama"><pre class="mermaid">${escaparHtml(text)}</pre></figure>\n`;
      }
      return false; // usa el renderizador por defecto (con resaltado)
    },
    image({ href, title, text }) {
      const rutaLocal = path.resolve(raizGuia, href);
      const pie = title ? `<figcaption>${title}</figcaption>` : '';
      if (!/^https?:/.test(href) && !fs.existsSync(rutaLocal)) {
        // Captura todavía no agregada: mostramos un recuadro que lo indica.
        return `<figure class="captura-pendiente"><div><strong>📷 Captura pendiente</strong><br>${text}<br><code>${href}</code></div>${pie}</figure>`;
      }
      const src = /^https?:/.test(href) ? href : pathToFileURL(rutaLocal).href;
      return `<figure class="captura"><img src="${src}" alt="${text}">${pie}</figure>`;
    }
  }
});

// Bloques especiales: > [!EOP], > [!GENERAL], > [!ADVERTENCIA], > [!CONSEJO], > [!NOTA],
// > [!RECONSTRUIDO], > [!PENDIENTE], > [!PRUEBA]
const tiposAviso = {
  EOP: { clase: 'eop', titulo: 'En EOP' },
  GENERAL: { clase: 'general', titulo: 'En un proyecto nuevo' },
  ADVERTENCIA: { clase: 'advertencia', titulo: 'Atención' },
  CONSEJO: { clase: 'consejo', titulo: 'Consejo' },
  NOTA: { clase: 'nota', titulo: 'Nota' },
  RECONSTRUIDO: { clase: 'reconstruido', titulo: 'Paso reconstruido' },
  PENDIENTE: { clase: 'pendiente', titulo: 'Todavía no implementado' },
  PRUEBA: { clase: 'prueba', titulo: 'Comprobá que funciona' }
};

function convertirAvisos(html) {
  return html.replace(/<blockquote>\s*<p>\[!([A-Z]+)\](?:[ \t]*([^\n<]*))?\n?/g, (todo, tipo, tituloPropio) => {
    const t = tiposAviso[tipo];
    if (!t) return todo;
    const titulo = tituloPropio?.trim() || t.titulo;
    return `<blockquote class="aviso aviso-${t.clase}"><div class="aviso-titulo">${titulo}</div><p>`;
  }).replace(/<p>\s*<\/p>/g, '');
}

function escaparHtml(s) {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

let cuerpo = '';
for (const archivo of config.capitulos) {
  const rutaCapitulo = path.join(raizGuia, archivo);
  if (!fs.existsSync(rutaCapitulo)) {
    console.warn(`AVISO: falta ${archivo}; se omite.`);
    continue;
  }
  const markdown = fs.readFileSync(rutaCapitulo, 'utf8');
  const clase = path.basename(archivo).startsWith('00') ? 'capitulo capitulo-intro' : 'capitulo';
  cuerpo += `<section class="${clase}">\n${convertirAvisos(marked.parse(markdown))}\n</section>\n`;
}

// ---------- Portada e índice ----------

const portada = `
<section class="portada">
  <div class="portada-banda">
    <div class="portada-etiqueta">Manual de capacitación</div>
    <h1 class="portada-titulo">${config.titulo}</h1>
    <p class="portada-subtitulo">${config.subtitulo}</p>
  </div>
  <div class="portada-cuerpo">
    <p class="portada-proyecto">${config.proyecto}</p>
    <div class="portada-tareas">
      <div class="portada-tareas-titulo">Tareas documentadas</div>
      <ul>${config.tareasDocumentadas.map(t => `<li>${t}</li>`).join('')}</ul>
    </div>
    <table class="portada-datos">
      <tr><th>Versión</th><td>${config.version}</td></tr>
      <tr><th>Fecha</th><td>${config.fecha}</td></tr>
      <tr><th>Stack</th><td>ASP.NET Core MVC · .NET 10 · Microsoft.Identity.Web 4.16.1</td></tr>
      <tr><th>Fuente editable</th><td><code>docs/guias/autenticacion-entra-id/</code> (Markdown)</td></tr>
    </table>
  </div>
</section>`;

const tablaIndice = `
<section class="indice">
  <h1 class="indice-titulo">Índice</h1>
  <ol class="indice-lista">
    ${indice.map(e => `<li class="nivel-${e.nivel}"><a href="#${e.id}">${e.texto}</a></li>`).join('\n    ')}
  </ol>
</section>`;

const css = fs.readFileSync(path.join(aqui, 'estilos.css'), 'utf8');
const cssResaltado = fs.readFileSync(path.join(aqui, 'node_modules', 'highlight.js', 'styles', 'github.css'), 'utf8');
const rutaMermaid = pathToFileURL(path.join(aqui, 'node_modules', 'mermaid', 'dist', 'mermaid.min.js')).href;

const html = `<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8">
<title>${config.titulo}</title>
<style>${cssResaltado}</style>
<style>${css}</style>
<script src="${rutaMermaid}"></script>
</head>
<body>
${portada}
${tablaIndice}
${cuerpo}
<script>
  mermaid.initialize({
    startOnLoad: false,
    theme: 'base',
    securityLevel: 'loose',
    fontFamily: 'Segoe UI, Arial, sans-serif',
    themeVariables: {
      primaryColor: '#e8f0fb', primaryBorderColor: '#2b5797', primaryTextColor: '#1b2a41',
      lineColor: '#2b5797', secondaryColor: '#fff4e0', tertiaryColor: '#eef7ee',
      actorBkg: '#e8f0fb', actorBorder: '#2b5797', noteBkgColor: '#fff8d6', noteBorderColor: '#c9a227',
      fontSize: '17px'
    },
    sequence: { mirrorActors: false, useMaxWidth: false, wrap: true, width: 170, actorMargin: 40, messageMargin: 32, messageFontSize: 15, noteFontSize: 14, actorFontSize: 15 },
    flowchart: { useMaxWidth: false, htmlLabels: true, curve: 'basis', wrappingWidth: 320, nodeSpacing: 35, rankSpacing: 40, padding: 12 }
  });
  // Cada diagrama se muestra a su tamaño natural, achicándolo solo si no entra
  // en el ancho o en el alto disponible de una página A4.
  function ajustarDiagramas() {
    const anchoMaximo = 650, altoMaximo = 880;
    document.querySelectorAll('figure.diagrama svg').forEach(svg => {
      const caja = svg.viewBox.baseVal;
      if (!caja || !caja.width) return;
      const escala = Math.min(anchoMaximo / caja.width, altoMaximo / caja.height, 1);
      svg.removeAttribute('width');
      svg.style.maxWidth = 'none';
      svg.style.width = (caja.width * escala) + 'px';
      svg.style.height = (caja.height * escala) + 'px';
    });
  }
  window.diagramasListos = mermaid.run({ querySelector: '.mermaid' })
    .then(() => { ajustarDiagramas(); return true; })
    .catch(e => { console.error('MERMAID', e); return false; });
</script>
</body>
</html>`;

const rutaHtml = path.join(carpetaSalida, 'guia.html');
fs.writeFileSync(rutaHtml, html, 'utf8');
console.log('HTML generado:', rutaHtml);

// ---------- HTML -> PDF ----------

const navegador = await puppeteer.launch({ executablePath: buscarNavegador(), headless: true, args: ['--allow-file-access-from-files'] });
const pagina = await navegador.newPage();
pagina.on('console', m => { if (m.type() === 'error') console.error('NAVEGADOR:', m.text()); });
await pagina.goto(pathToFileURL(rutaHtml).href, { waitUntil: 'networkidle0', timeout: 120000 });
const diagramasOk = await pagina.evaluate(() => window.diagramasListos);
if (!diagramasOk) throw new Error('Algún diagrama Mermaid no se pudo dibujar. Revisá la consola.');

const rutaPdf = path.join(carpetaSalida, config.archivoSalida);
await pagina.pdf({
  path: rutaPdf,
  format: 'A4',
  printBackground: true,
  outline: true,
  tagged: true,
  margin: { top: '18mm', bottom: '18mm', left: '16mm', right: '16mm' },
  displayHeaderFooter: true,
  headerTemplate: `<div style="width:100%;font-family:Segoe UI,Arial;font-size:8px;color:#7a869a;padding:0 16mm;text-align:right;">${config.titulo}</div>`,
  footerTemplate: `<div style="width:100%;font-family:Segoe UI,Arial;font-size:8px;color:#7a869a;padding:0 16mm;display:flex;justify-content:space-between;"><span>Guía EOP · v${config.version}</span><span>Página <span class="pageNumber"></span> de <span class="totalPages"></span></span></div>`
});
await navegador.close();
console.log('PDF generado:', rutaPdf);

function buscarNavegador() {
  const candidatos = [
    process.env.CHROME_PATH,
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
    '/usr/bin/google-chrome', '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'
  ].filter(Boolean);
  const encontrado = candidatos.find(p => fs.existsSync(p));
  if (!encontrado) throw new Error('No encontré Chrome ni Edge. Definí la variable CHROME_PATH.');
  return encontrado;
}
