import express from 'express';
import { verifyCedula } from './cedula-scraper.js';

const app = express();
const PORT = process.env.PORT ?? 3000;
const INTERNAL_KEY = process.env.INTERNAL_KEY ?? '';

app.use(express.json());

// All routes require the shared internal key — never expose this service to the public.
app.use((req, res, next) => {
  const key = req.headers['x-internal-key'];
  if (!INTERNAL_KEY || key !== INTERNAL_KEY) {
    return res.status(401).json({ error: 'Unauthorized' });
  }
  next();
});

app.post('/verify', async (req, res) => {
  const { licenseNumber } = req.body ?? {};

  if (!licenseNumber || typeof licenseNumber !== 'string' || !licenseNumber.trim()) {
    return res.status(400).json({ error: 'licenseNumber is required' });
  }

  try {
    const result = await verifyCedula(licenseNumber.trim());
    return res.json(result);
  } catch (err) {
    console.error('[/verify] Unexpected error:', err);
    return res.status(500).json({ isValid: false });
  }
});

app.listen(PORT, () => {
  console.log(`cedula-scraper listening on port ${PORT}`);
});
