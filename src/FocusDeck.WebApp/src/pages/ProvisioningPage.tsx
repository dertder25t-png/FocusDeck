import { useState } from 'react'
import QrReader from 'react-qr-reader'
import QRCode from 'qrcode.react'

export function ProvisioningPage() {
  const [mode, setMode] = useState<'scan' | 'display'>('display');
  const [scannedData, setScannedData] = useState<string | null>(null);

  const handleScan = (result: any, error: any) => {
    if (!!result) {
      setScannedData(result?.text);
    }

    if (!!error) {
      console.info(error);
    }
  };

  const oneTimeCode = "GENERATED_ONE_TIME_CODE"; // This should be fetched from the server

  return (
    <div>
      <h1>Device Provisioning</h1>
      <div>
        <button onClick={() => setMode('display')}>Display QR Code</button>
        <button onClick={() => setMode('scan')}>Scan QR Code</button>
      </div>

      {mode === 'display' && (
        <div>
          <h2>Scan this QR code with your new device</h2>
          <QRCode value={oneTimeCode} />
        </div>
      )}

      {mode === 'scan' && (
        <div>
          <h2>Scan the QR code from your other device</h2>
          <QrReader
            onResult={handleScan}
            constraints={{ facingMode: 'environment' }}
            containerStyle={{ width: '100%' }}
          />
          {scannedData && <p>Scanned Data: {scannedData}</p>}
        </div>
      )}
    </div>
  );
}
