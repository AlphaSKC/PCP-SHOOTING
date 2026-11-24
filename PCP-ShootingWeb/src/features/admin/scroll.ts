/** En tableta y celular el editor está debajo de la lista: al elegir algo, baja hasta él. */
export function revealEditor(id = 'editor') {
  if (window.matchMedia('(max-width: 1023px)').matches) {
    requestAnimationFrame(() => document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
  }
}
