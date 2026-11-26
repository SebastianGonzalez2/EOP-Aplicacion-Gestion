// Saca capturas reales de la app EOP corriendo en local (perfil https).
// Uso: levantar la app con `dotnet run --project src/EOP.Gestion.WebApp --launch-profile https`
// y después, desde esta carpeta, `npm run capturas`.
//
// Solo captura pantallas que se pueden ver SIN iniciar sesión: el script nunca
// escribe credenciales. Las capturas del portal de Azure se agregan a mano
// (ver README.md de la guía).

import puppeteer from 'puppeteer-core';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';

const aqui = path.dirname(fileURLToPath(import.meta.url));
const carpetaImagenes = path.resolve(aqui, '..', 'imagenes', 'app');
fs.mkdirSync(carpetaImagenes, { recursive: true });

const base = process.env.EOP_URL ?? 'https://localhost:7125';
const navegador = await puppeteer.launch({
  executablePath: buscarNavegador(),
  headless: true,
  acceptInsecureCerts: true,
  args: ['--lang=es-UY']
});

const capturas = [
  { archivo: 'sesion-cerrada.png', ruta: '/Cuenta/SesionCerrada', ancho: 1280, alto: 560 },
  { archivo: 'sesion-cerrada-celular.png', ruta: '/Cuenta/SesionCerrada', ancho: 390, alto: 700 },
  { archivo: 'error-inicio-sesion.png', ruta: '/Cuenta/ErrorInicioSesion', ancho: 1280, alto: 600 },
  // Sin sesión, "/" redirige al login real de Microsoft Entra ID.
  { archivo: 'login-microsoft.png', ruta: '/', ancho: 1280, alto: 760, esperarHost: 'login.microsoftonline.com' }
];

for (const c of capturas) {
  const pagina = await navegador.newPage();
  await pagina.setViewport({ width: c.ancho, height: c.alto, deviceScaleFactor: 2 });
  await pagina.goto(base + c.ruta, { waitUntil: 'networkidle0', timeout: 60000 });

  if (c.esperarHost && !new URL(pagina.url()).host.includes(c.esperarHost)) {
    throw new Error(`Se esperaba terminar en ${c.esperarHost} y se terminó en ${pagina.url()}`);
  }

  await new Promise(r => setTimeout(r, 1500));
  await pagina.screenshot({ path: path.join(carpetaImagenes, c.archivo) });
  console.log(`OK ${c.archivo} <- ${pagina.url().split('?')[0]}`);
  await pagina.close();
}

await navegador.close();

function buscarNavegador() {
  const candidatos = [
    process.env.CHROME_PATH,
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe'
  ].filter(Boolean);
  const encontrado = candidatos.find(p => fs.existsSync(p));
  if (!encontrado) throw new Error('No encontré Chrome ni Edge. Definí CHROME_PATH.');
  return encontrado;
}
