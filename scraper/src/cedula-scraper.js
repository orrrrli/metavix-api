import { chromium } from 'playwright';

const SEP_URL =
  'https://www.cedulaprofesional.sep.gob.mx/cedula/presidencia/indexAvanzada.action';
const TIMEOUT_MS = 30_000;

// Reuse a single browser instance across requests to avoid cold-start overhead.
let browser = null;

async function getBrowser() {
  if (!browser || !browser.isConnected()) {
    browser = await chromium.launch({
      headless: true,
      args: ['--no-sandbox', '--disable-setuid-sandbox'],
    });
  }
  return browser;
}

/**
 * Searches the SEP cédula professional registry for the given license number.
 *
 * Returns { isValid: true, nombre, apellidoPaterno, apellidoMaterno, institucion, carrera }
 * or     { isValid: false }
 *
 * NOTE: If the SEP site updates its DOM, adjust the selectors below.
 * The expected form field is <input name="numeroCedula"> on the advanced search page.
 * Results appear as rows in the first <table> on the results page.
 */
export async function verifyCedula(licenseNumber) {
  const b = await getBrowser();
  const page = await b.newPage();

  try {
    await page.goto(SEP_URL, { waitUntil: 'networkidle', timeout: TIMEOUT_MS });

    // Fill the cédula number and submit.
    await page.locator('input[name="numeroCedula"]').fill(licenseNumber);
    await page.locator('input[type="submit"], button[type="submit"]').first().click();
    await page.waitForLoadState('networkidle', { timeout: TIMEOUT_MS });

    const bodyText = await page.textContent('body');

    // SEP shows one of these strings when no records are found.
    const noResults =
      bodyText?.includes('No se encontraron') ||
      bodyText?.includes('no existen registros') ||
      bodyText?.includes('0 registros') ||
      bodyText?.includes('Sin resultados');

    if (noResults) return { isValid: false };

    // Grab the first result row.
    const firstRow = page.locator('table tbody tr').first();
    if (!(await firstRow.count())) return { isValid: false };

    const cells = await firstRow.locator('td').allTextContents();
    if (!cells.length) return { isValid: false };

    // Expected column order: Nombre(s) | Apellido Paterno | Apellido Materno | Institución | Carrera | Tipo | Nº Cédula
    return {
      isValid: true,
      nombre: cells[0]?.trim() ?? '',
      apellidoPaterno: cells[1]?.trim() ?? '',
      apellidoMaterno: cells[2]?.trim() ?? '',
      institucion: cells[3]?.trim() ?? '',
      carrera: cells[4]?.trim() ?? '',
    };
  } catch (err) {
    console.error('[cedula-scraper] Error verifying license', licenseNumber, ':', err.message);
    return { isValid: false };
  } finally {
    await page.close();
  }
}
