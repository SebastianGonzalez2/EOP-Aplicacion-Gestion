// Convierte páginas del PDF generado en imágenes PNG para revisarlo sin abrir un visor.
// Uso: node vista-previa.mjs 1,2,5-7     (sin argumentos: todas las páginas)
// Salida: ../salida/vista-previa/pagina-NN.png

import puppeteer from 'puppeteer-core';
import { fileURLToPath, pathToFileURL } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';

const aqui = path.dirname(fileURLToPath(import.meta.url));
const raizGuia = path.resolve(aqui, '..');
const config = JSON.parse(fs.readFileSync(path.join(raizGuia, 'guia.json'), 'utf8'));
const rutaPdf = path.join(raizGuia, 'salida', config.archivoSalida);
const carpeta = path.join(raizGuia, 'salida', 'vista-previa');
fs.mkdirSync(carpeta, { recursive: true });

const pdfjs = pathToFileURL(path.join(aqui, 'node_modules', 'pdfjs-dist', 'build', 'pdf.min.mjs')).href;
const worker = pathToFileURL(path.join(aqui, 'node_modules', 'pdfjs-dist', 'build', 'pdf.worker.min.mjs')).href;
const datos = fs.readFileSync(rutaPdf).toString('base64');

const navegador = await puppeteer.launch({
  executablePath: ['C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe', 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe'].find(p => fs.existsSync(p)),
  headless: true,
  args: ['--allow-file-access-from-files']
});
const pagina = await navegador.newPage();
await pagina.goto(pathToFileURL(path.join(aqui, 'estilos.css')).href);
const total = await pagina.evaluate(async (pdfjs, worker, datos) => {
  const lib = await import(pdfjs);
  lib.GlobalWorkerOptions.workerSrc = worker;
  const bytes = Uint8Array.from(atob(datos), c => c.charCodeAt(0));
  window.doc = await lib.getDocument({ data: bytes }).promise;
  return window.doc.numPages;
}, pdfjs, worker, datos);

const pedidas = (process.argv[2] ?? `1-${total}`).split(',').flatMap(r => {
  const [a, b] = r.split('-').map(Number);
  return Array.from({ length: (b || a) - a + 1 }, (_, i) => a + i);
}).filter(n => n >= 1 && n <= total);

for (const n of pedidas) {
  const png = await pagina.evaluate(async n => {
    const p = await window.doc.getPage(n);
    const vista = p.getViewport({ scale: 1.4 });
    const lienzo = document.createElement('canvas');
    lienzo.width = vista.width; lienzo.height = vista.height;
    await p.render({ canvasContext: lienzo.getContext('2d'), viewport: vista }).promise;
    return lienzo.toDataURL('image/png').split(',')[1];
  }, n);
  const destino = path.join(carpeta, `pagina-${String(n).padStart(2, '0')}.png`);
  fs.writeFileSync(destino, Buffer.from(png, 'base64'));
}
console.log(`Páginas totales: ${total}. Generadas: ${pedidas.join(', ')} en ${carpeta}`);

// Con --miniaturas, arma además hojas de 12 páginas para revisar el diseño de un vistazo.
if (process.argv.includes('--miniaturas')) {
  await pagina.setViewport({ width: 1800, height: 1700 });
  for (let i = 0; i < pedidas.length; i += 12) {
    const grupo = pedidas.slice(i, i + 12);
    const celdas = grupo.map(n => {
      const img = pathToFileURL(path.join(carpeta, `pagina-${String(n).padStart(2, '0')}.png`)).href;
      return `<div style="background:white;position:relative"><img src="${img}" style="width:100%;display:block"><span style="position:absolute;top:2px;left:4px;font:bold 22px Arial;color:#c00">${n}</span></div>`;
    }).join('');
    const archivoHtml = path.join(carpeta, `hoja-${String(i / 12 + 1).padStart(2, '0')}.html`);
    fs.writeFileSync(archivoHtml, `<body style="margin:0;background:#888;display:grid;grid-template-columns:repeat(6,1fr);gap:6px;padding:6px">${celdas}</body>`);
    await pagina.goto(pathToFileURL(archivoHtml).href, { waitUntil: 'load' });
    await pagina.screenshot({ path: archivoHtml.replace('.html', '.png'), fullPage: true });
  }
  console.log('Hojas de miniaturas generadas.');
}
await navegador.close();
