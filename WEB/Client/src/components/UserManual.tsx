import { X } from "lucide-react";
import manualPdfUrl from "../../../MANUAL/USER_MANUAL.pdf?url";

type UserManualProps = {
  onClose: () => void;
};

export function UserManual({ onClose }: UserManualProps) {
  return (
    <div className="manual-backdrop" role="presentation" onMouseDown={onClose}>
      <section
        className="manual-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="manual-title"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="manual-header">
          <div>
            <h2 id="manual-title">Transgraphier 2.4.1 User Manual</h2>
            <p>Digital Instrumental Trans-Communication (ITC) workbench</p>
          </div>
          <button type="button" className="manual-close" onClick={onClose} title="Close manual">
            <X size={18} aria-hidden="true" />
          </button>
        </header>

        <div className="manual-pdf-container">
          <object
            className="manual-pdf"
            data={`${manualPdfUrl}#view=FitH`}
            type="application/pdf"
            aria-label="Transgraphier user manual PDF"
          >
            <div className="manual-pdf-fallback">
              <p>This browser cannot display the manual PDF inline.</p>
              <a href={manualPdfUrl} target="_blank" rel="noreferrer">Open the PDF in a new window</a>
            </div>
          </object>
        </div>
      </section>
    </div>
  );
}
