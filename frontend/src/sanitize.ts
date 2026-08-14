/**
 * Removes executable or interactive markup from Teams message HTML before it
 * is inserted into the page. Safe links are opened outside the dashboard.
 */
export function sanitizeHtml(html?: string) {
  if (!html) return "<p>No message content</p>";

  const document = new DOMParser().parseFromString(html, "text/html");

  // Remove elements that can execute code or submit information.
  document
    .querySelectorAll(
      "script, iframe, object, embed, form, input, button, style, link, meta",
    )
    .forEach((node) => node.remove());

  // Remove inline event handlers and javascript: URLs from remaining elements.
  document.body.querySelectorAll("*").forEach((element) => {
    for (const attribute of [...element.attributes]) {
      const name = attribute.name.toLowerCase();
      const value = attribute.value.trim().toLowerCase();
      if (
        name.startsWith("on") ||
        name === "srcdoc" ||
        ((name === "href" || name === "src") && value.startsWith("javascript:"))
      )
        element.removeAttribute(attribute.name);
    }

    // Hosted images are rendered separately through authenticated API requests.
    if (
      element.tagName === "IMG" &&
      element.getAttribute("src")?.includes("hostedContents")
    ) {
      element.remove();
      return;
    }

    // Prevent a message link from replacing the dashboard or accessing its tab.
    if (element.tagName === "A") {
      element.setAttribute("target", "_blank");
      element.setAttribute("rel", "noopener noreferrer");
    }
  });
  return document.body.innerHTML;
}
