import { Download, ImageOff, LoaderCircle } from "lucide-react";
import { useEffect, useState } from "react";
import type { DownloadedMedia } from "../api";

export interface MessageMediaSource {
  id: string;
  contentType?: string;
  fileName?: string;
  download: () => Promise<DownloadedMedia>;
}

/** Downloads protected media with the JWT and manages its temporary blob URL. */
function MediaItem({ source }: { source: MessageMediaSource }) {
  const [url, setUrl] = useState("");
  const [error, setError] = useState(false);
  const [isImage, setIsImage] = useState(false);
  const [downloadName, setDownloadName] = useState(source.fileName);

  useEffect(() => {
    let active = true;
    let objectUrl = "";

    source
      .download()
      .then(({ blob, fileName }) => {
        if (!active) return;
        objectUrl = URL.createObjectURL(blob);
        setIsImage(
          blob.type.startsWith("image/") ||
            source.contentType?.startsWith("image/") === true,
        );
        setDownloadName(fileName || source.fileName);
        setUrl(objectUrl);
      })
      .catch(() => {
        if (active) setError(true);
      });

    return () => {
      active = false;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [source.id]);

  if (error) {
    return (
      <span className="media-error" title="Media could not be loaded">
        <ImageOff />
      </span>
    );
  }

  if (!url) {
    return (
      <span className="media-loading">
        <LoaderCircle className="spin" />
      </span>
    );
  }

  if (isImage) {
    return (
      <a href={url} target="_blank" rel="noreferrer">
        <img src={url} alt={source.fileName || "Teams message attachment"} />
      </a>
    );
  }

  return (
    <a className="media-download" href={url} download={downloadName || true}>
      <Download /> {downloadName || "Download attachment"}
    </a>
  );
}

/** Renders all downloadable media belonging to one message. */
export function MessageMedia({ sources }: { sources: MessageMediaSource[] }) {
  if (sources.length === 0) return null;

  return (
    <div className="message-media">
      {sources.map((source) => (
        <MediaItem key={source.id} source={source} />
      ))}
    </div>
  );
}
