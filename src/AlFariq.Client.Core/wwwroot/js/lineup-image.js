// Image d'une composition (US-011), dessinée sur un canevas dans le navigateur : rien n'est envoyé à un serveur.
// Reçoit la carte préparée par LineupCardBuilder. Les couleurs d'équipe viennent de la feuille de style de l'appli.

const WIDTH = 1080;
const MARGIN = 40;
const GAP = 24;
const PADDING = 24;
const ROW_HEIGHT = 190;
const FONT = 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif';

// Thème clair fixe : l'image se lit de la même façon chez tous les destinataires.
const COLORS = {
  background: "#f5f7f6",
  surface: "#ffffff",
  text: "#17211c",
  muted: "#55645c",
  border: "#b9c6bf",
  primary: "#0b6e4f",
  pitch: "#1f6b3a",
  chalk: "rgba(255, 255, 255, 0.45)",
};

export async function renderLineup(card) {
  const canvas = document.createElement("canvas");
  const context = canvas.getContext("2d");

  // Un premier passage mesure la hauteur (le dessin hors du canevas est sans effet), le second dessine.
  const height = Math.ceil(paint(context, card));
  canvas.width = WIDTH;
  canvas.height = height;
  context.fillStyle = COLORS.background;
  context.fillRect(0, 0, WIDTH, height);
  paint(context, card);

  return new Promise((resolve, reject) => canvas.toBlob(
    blob => (blob ? resolve(blob) : reject(new Error("L'image n'a pas pu être créée."))),
    "image/png"));
}

function paint(context, card) {
  let y = header(context, card) + GAP;

  // Deux listes tiennent côte à côte ; un terrain prend toute la largeur.
  const sideBySide = card.teams.length === 2 && card.teams.every(team => team.lines.length === 0);
  if (sideBySide) {
    const width = (WIDTH - 2 * MARGIN - GAP) / 2;
    const lefts = [MARGIN, MARGIN + width + GAP];
    const bottom = Math.max(...card.teams.map((team, i) => teamBody(context, card, team, lefts[i], y, width)));
    card.teams.forEach((team, i) => {
      panel(context, lefts[i], y, width, bottom - y);
      teamBody(context, card, team, lefts[i], y, width);
    });
    y = bottom + GAP;
  } else {
    for (const team of card.teams) {
      const width = WIDTH - 2 * MARGIN;
      const bottom = teamBody(context, card, team, MARGIN, y, width);
      panel(context, MARGIN, y, width, bottom - y);
      teamBody(context, card, team, MARGIN, y, width);
      y = bottom + GAP;
    }
  }

  if (card.unassigned.length > 0) {
    const bottom = unassigned(context, card, y);
    panel(context, MARGIN, y, WIDTH - 2 * MARGIN, bottom - y);
    unassigned(context, card, y);
    y = bottom + GAP;
  }

  setFont(context, 28, 600);
  write(context, ["AlFariq"], WIDTH / 2, y, 36, COLORS.muted, "center");
  return y + 36 + GAP;
}

function header(context, card) {
  const width = WIDTH - 2 * MARGIN;
  setFont(context, 52, 700);
  const title = wrap(context, capitalize(card.title), width, 3);
  setFont(context, 36, 400);
  const details = wrap(context, card.details, width, 2);
  const location = card.location ? wrap(context, card.location, width, 3) : [];
  const height = 40 + title.length * 62 + (details.length + location.length) * 46 + 32;

  context.fillStyle = COLORS.primary;
  context.fillRect(0, 0, WIDTH, height);

  let y = 40;
  setFont(context, 52, 700);
  y = write(context, title, MARGIN, y, 62, "#ffffff");
  setFont(context, 36, 400);
  write(context, [...details, ...location], MARGIN, y, 46, "#ffffff");
  return height;
}

/** Dessine le contenu d'une équipe et rend le bas de ce qu'il occupe. */
function teamBody(context, card, team, x, y, width) {
  const color = teamColor(team.colorCode);
  const inner = width - 2 * PADDING;

  setFont(context, 38, 700);
  const heading = wrap(context, team.formation ? `${team.name} · ${team.formation}` : team.name, inner, 2);
  const bandHeight = 28 + heading.length * 48;
  roundedRect(context, x + 6, y + 6, width - 12, bandHeight, 14);
  context.fillStyle = color.fill;
  context.fill();
  // Contour : une équipe en blanc reste visible sur le fond blanc.
  context.strokeStyle = COLORS.border;
  context.lineWidth = 2;
  context.stroke();
  write(context, heading, x + PADDING, y + 22, 48, color.ink);
  let bottom = y + 6 + bandHeight + 16;

  if (team.lines.length > 0) {
    bottom = pitch(context, team, x + 6, bottom, width - 12, color);
    if (team.othersTitle) {
      setFont(context, 32, 700);
      bottom = write(context, [team.othersTitle], x + PADDING, bottom + 16, 42, COLORS.muted);
      setFont(context, 32, 400);
      const others = wrap(context, team.others.map(describe).join(", "), inner);
      bottom = write(context, others, x + PADDING, bottom, 42, COLORS.text);
    }
  } else if (team.players.length === 0) {
    setFont(context, 32, 400);
    bottom = write(context, wrap(context, card.emptyTeam, inner), x + PADDING, bottom, 42, COLORS.muted);
  } else {
    for (const player of team.players) {
      setFont(context, 34, 600);
      bottom = write(context, wrap(context, player.name, inner, 2), x + PADDING, bottom, 42, COLORS.text);
      if (player.position) {
        setFont(context, 28, 400);
        bottom = write(context, wrap(context, player.position, inner, 1), x + PADDING, bottom, 34, COLORS.muted);
      }

      bottom += 14;
    }
  }

  return bottom + 16;
}

function pitch(context, team, x, y, width, color) {
  const rows = [...team.lines].reverse(); // l'attaque en haut, le but en bas
  const height = rows.length * ROW_HEIGHT + 32;

  roundedRect(context, x, y, width, height, 14);
  context.fillStyle = COLORS.pitch;
  context.fill();

  // Tracé schématique : ligne médiane, rond central et surface de réparation.
  context.strokeStyle = COLORS.chalk;
  context.lineWidth = 4;
  context.beginPath();
  context.moveTo(x, y + height / 2);
  context.lineTo(x + width, y + height / 2);
  context.stroke();
  context.beginPath();
  context.arc(x + width / 2, y + height / 2, width * 0.12, 0, 2 * Math.PI);
  context.stroke();
  context.strokeRect(x + width * 0.22, y + height - 130, width * 0.56, 130);

  rows.forEach((row, rowIndex) => {
    const spotWidth = width / row.length;
    row.forEach((spot, i) => {
      const centre = x + spotWidth * (i + 0.5);
      const top = y + 24 + rowIndex * ROW_HEIGHT;
      marker(context, spot, centre, top, color);
      if (spot.name) {
        nameTag(context, spot.name, centre, top + 90, Math.min(spotWidth - 24, 320));
      }
    });
  });

  return y + height;
}

function marker(context, spot, centre, top, color) {
  const free = !spot.name;
  context.beginPath();
  context.arc(centre, top + 40, 40, 0, 2 * Math.PI);
  context.fillStyle = free ? "rgba(0, 0, 0, 0.25)" : color.fill;
  context.fill();
  context.setLineDash(free ? [12, 10] : []);
  context.strokeStyle = "#ffffff";
  context.lineWidth = 4;
  context.stroke();
  context.setLineDash([]);

  setFont(context, 26, 700);
  write(context, [spot.position], centre, top + 26, 30, free ? "#ffffff" : color.ink, "center");
}

function nameTag(context, name, centre, top, maxWidth) {
  setFont(context, 28, 600);
  const lines = wrap(context, name, maxWidth, 2);
  const width = Math.max(...lines.map(line => context.measureText(line).width)) + 20;
  roundedRect(context, centre - width / 2, top, width, lines.length * 34 + 8, 10);
  context.fillStyle = COLORS.surface;
  context.fill();
  write(context, lines, centre, top + 6, 34, COLORS.text, "center");
}

function unassigned(context, card, y) {
  const inner = WIDTH - 2 * MARGIN - 2 * PADDING;
  setFont(context, 34, 700);
  let bottom = write(
    context, wrap(context, card.unassignedTitle, inner), MARGIN + PADDING, y + PADDING, 44, COLORS.text);
  setFont(context, 32, 400);
  bottom = write(
    context, wrap(context, card.unassigned.join(", "), inner), MARGIN + PADDING, bottom + 4, 42, COLORS.text);
  return bottom + PADDING;
}

// ---------- Outils de dessin ----------

function describe(player) {
  return player.position ? `${player.name} (${player.position})` : player.name;
}

function capitalize(text) {
  return text.charAt(0).toLocaleUpperCase() + text.slice(1);
}

function teamColor(code) {
  const probe = document.createElement("span");
  probe.className = `team-color--${code}`;
  probe.hidden = true;
  document.body.append(probe);
  const style = getComputedStyle(probe);
  const fill = style.getPropertyValue("--team-color").trim() || COLORS.muted;
  const ink = style.getPropertyValue("--team-ink").trim() || "#ffffff";
  probe.remove();
  return { fill, ink };
}

function setFont(context, size, weight) {
  context.font = `${weight} ${size}px ${FONT}`;
}

/** Écrit des lignes et rend la position qui suit la dernière. */
function write(context, lines, x, y, lineHeight, color, align = "left") {
  context.fillStyle = color;
  context.textAlign = align;
  context.textBaseline = "top";
  lines.forEach((line, i) => context.fillText(line, x, y + i * lineHeight));
  return y + lines.length * lineHeight;
}

/** Découpe un texte à la largeur donnée ; au-delà de maxLines, la dernière ligne finit par « … ». */
export function wrap(context, text, maxWidth, maxLines = Infinity) {
  const fits = candidate => context.measureText(candidate).width <= maxWidth;
  const lines = [];
  let line = "";

  for (const { part, glue } of breakable(String(text))) {
    const candidate = line ? `${line}${glue}${part}` : part;
    if (fits(candidate)) {
      line = candidate;
      continue;
    }

    if (line) {
      lines.push(line);
    }

    // Un morceau plus large que la place disponible est coupé.
    line = "";
    for (const character of part) {
      if (line && !fits(line + character)) {
        lines.push(line);
        line = "";
      }

      line += character;
    }
  }

  if (line) {
    lines.push(line);
  }

  if (lines.length <= maxLines) {
    return lines;
  }

  const kept = lines.slice(0, maxLines);
  const last = Array.from(kept[maxLines - 1]);
  while (last.length > 0 && !fits(`${last.join("")}…`)) {
    last.pop();
  }

  kept[maxLines - 1] = `${last.join("")}…`;
  return kept;
}

// On revient à la ligne entre deux mots, ou après un trait d'union (« Jean- / Christophe »).
function breakable(text) {
  return text.split(/\s+/).filter(Boolean).flatMap(word =>
    word.match(/[^-]*-|[^-]+/g).map((part, i) => ({ part, glue: i === 0 ? " " : "" })));
}

function panel(context, x, y, width, height) {
  roundedRect(context, x, y, width, height, 18);
  context.fillStyle = COLORS.surface;
  context.fill();
  context.strokeStyle = COLORS.border;
  context.lineWidth = 2;
  context.stroke();
}

// Écrit à la main : CanvasRenderingContext2D.roundRect manque sur d'anciens navigateurs.
function roundedRect(context, x, y, width, height, radius) {
  context.beginPath();
  context.moveTo(x + radius, y);
  context.arcTo(x + width, y, x + width, y + height, radius);
  context.arcTo(x + width, y + height, x, y + height, radius);
  context.arcTo(x, y + height, x, y, radius);
  context.arcTo(x, y, x + width, y, radius);
  context.closePath();
}
