// Partage d'une composition (US-011) : partage natif, presse-papiers et image. Tout reste sur l'appareil jusqu'à
// ce que l'utilisateur choisisse une application. Chargé comme module ES par BrowserShareService.

import { renderLineup } from "./lineup-image.js";

export function canShare() {
  return typeof navigator.share === "function";
}

// Doit partir directement du geste de l'utilisateur : aucune attente avant navigator.share.
async function share(data) {
  try {
    await navigator.share(data);
    return "shared";
  } catch (error) {
    return error?.name === "AbortError" ? "cancelled" : "failed";
  }
}

export function shareText(text) {
  return share({ text });
}

export async function copyText(text) {
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch {
    // Presse-papiers moderne absent (page non sécurisée) ou refusé : ancienne méthode.
    return copyFromSelection(text);
  }
}

function copyFromSelection(text) {
  const previous = document.activeElement;
  const area = document.createElement("textarea");
  area.value = text;
  area.setAttribute("readonly", "");
  area.style.position = "fixed";
  area.style.top = "0";
  area.style.opacity = "0";
  document.body.append(area);
  area.select();
  try {
    return document.execCommand("copy");
  } catch {
    return false;
  } finally {
    area.remove();
    previous?.focus?.();
  }
}

/** Dessine l'image et la garde prête : le partage peut ensuite partir sans délai. */
export async function createImage(card, fileName) {
  const blob = await renderLineup(card);
  const file = new File([blob], fileName, { type: "image/png" });
  const url = URL.createObjectURL(blob);

  let shareable = false;
  try {
    shareable = canShare() && navigator.canShare?.({ files: [file] }) === true;
  } catch {
    shareable = false;
  }

  return {
    info: () => ({ url, canShare: shareable }),
    share: () => share({ files: [file] }),
    dispose: () => URL.revokeObjectURL(url),
  };
}
