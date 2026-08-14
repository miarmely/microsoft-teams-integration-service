import { MessagesSquare } from "lucide-react";

/** Displays the shared product identity in the login page and navigation. */
export function Brand({ compact = false }: { compact?: boolean }) {
  return (
    <div className="brand">
      {/* Replace this .brand-mark element with: <img src="/company-logo.svg" alt="Company name" className="company-logo" /> */}
      <span className="brand-mark">
        <MessagesSquare size={22} />
      </span>
      {!compact && (
        <span>
          <strong>Teams Integration</strong>
          <small>Enterprise Hub</small>
        </span>
      )}
    </div>
  );
}
