const filters = document.querySelector(".filters");
const query = filters.querySelector("input");
const count = filters.querySelector("output");
const rows = [...document.querySelectorAll("#adr-table tbody tr")];
const number = new Intl.NumberFormat("fa-IR");

function normalize(value) {
  return value.toLowerCase()
    .replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - "۰".charCodeAt(0)))
    .replace(/[٠-٩]/g, (digit) => String(digit.charCodeAt(0) - "٠".charCodeAt(0)))
    .replace(/ي/g, "ی")
    .replace(/ك/g, "ک")
    .replace(/\u200c/g, "")
    .trim();
}

function filterRows() {
  const words = normalize(query.value).split(/\s+/);
  let matches = 0;

  for (const row of rows) {
    const text = normalize(row.textContent);
    row.hidden = !words.every((word) => text.includes(word));
    matches += Number(!row.hidden);
  }

  count.textContent = matches
    ? number.format(matches) + " تصمیم"
    : "تصمیمی با این مشخصات پیدا نشد";
}

filters.addEventListener("submit", (event) => event.preventDefault());
filters.addEventListener("input", filterRows);
filters.hidden = false;
filterRows();
