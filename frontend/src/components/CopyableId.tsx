import { Check, Copy } from "lucide-react";
import { useState } from "react";

/** Displays an identifier and copies its exact value to the system clipboard. */
export function CopyableId({ value }: { value: string }) {
  const [copied, setCopied] = useState(false);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1600);
    } catch {
      // Clipboard access can be denied outside HTTPS; selecting the text remains possible.
      setCopied(false);
    }
  };

  return (
    <div className="copyable-id">
      <code title={value}>{value}</code>
      <button type="button" onClick={() => void copy()} aria-label={`Copy ${value}`}>
        {copied ? <Check /> : <Copy />}
        <span>{copied ? "Copied" : "Copy"}</span>
      </button>
    </div>
  );
}
