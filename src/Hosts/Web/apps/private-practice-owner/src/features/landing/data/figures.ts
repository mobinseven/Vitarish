export const STYLE_BRIEF =
  "Technical blueprint engineering schematic - thin cobalt line-art on light drafting paper, exploded view, dotted leading lines, measurement ticks, no text labels.";

export type Figure = {
  src: string;
  alt: string;
  prompt: string;
};

export const figureTitles = {
  coreUnit: "CORE UNIT",
  topology: "TOPOLOGY",
  computeBoard: "COMPUTE BOARD",
} as const;

export function formatFigureLabel(id: string): string {
  return `FIG. ${id}`;
}

export function formatFigureRangeLabel(startId: string, endId: string): string {
  return `${formatFigureLabel(startId)}-${endId}`;
}

export function formatFigureTitleLabel(id: string, title: string): string {
  return `${formatFigureLabel(id)} - ${title}`;
}

export const figures = {
  hero: {
    src: "https://files.instantdb.com/fd13e78a-1a89-44ed-b1f3-3500218ed3f0/7/026d07f5-88c7-4fa9-94be-02dfff0c1c11?response-cache-control=public%2C%20max-age%3D86400%2C%20immutable&Policy=eyJTdGF0ZW1lbnQiOlt7IlJlc291cmNlIjoiaHR0cHM6Ly9maWxlcy5pbnN0YW50ZGIuY29tL2ZkMTNlNzhhLTFhODktNDRlZC1iMWYzLTM1MDAyMThlZDNmMC83LzAyNmQwN2Y1LTg4YzctNGZhOS05NGJlLTAyZGZmZjBjMWMxMT9yZXNwb25zZS1jYWNoZS1jb250cm9sPXB1YmxpYyUyQyUyMG1heC1hZ2UlM0Q4NjQwMCUyQyUyMGltbXV0YWJsZSIsIkNvbmRpdGlvbiI6eyJEYXRlTGVzc1RoYW4iOnsiQVdTOkVwb2NoVGltZSI6MTc4MzgxNDQwMH19fV19&Signature=hcwtIf5wpQAOhkCJdt5MqZwZI~b0r7sFl85rfOsAcIIf3l-Pe3lj4olYS6nPsIhP2nJvsvg~GscSbd19PZyIcPn7pNRB9xeIBOmMLMuwkx9sRC8SgpXHJZUrU7~rOnTtqmDeVX9yxogTW1m--eYctEJ~OdryVmZW1CR5C-sK2CdqSI6ee6liZ53eMyBS3ruQhcXXzLBnz0-jQYjGgimMR7-Xeo8YUwLOcvMop4~0RThb~eCMuDHPWDzpWOnMPJ1RF0natgJy1mSTfPIQMUcGKhBIyeoKQDyBUNllJvVTGaqqSy~7W5pGjgV1rraGNd8akEnLT4~2~3vwkJnK8t4v7A&Key-Pair-Id=K2D8SOWYIPDBRT",
    alt: "Exploded blueprint schematic of an AI accelerator server",
    prompt:
      "Exploded-view blueprint schematic of a futuristic AI accelerator server: top cover, PCB motherboard, accelerator chips, heatsinks and fans in floating layers, dotted leading lines, dimension annotations. Thin cobalt line-art on light drafting paper, isometric.",
  },
  deploy: {
    src: "https://files.instantdb.com/fd13e78a-1a89-44ed-b1f3-3500218ed3f0/0/86a290c1-9ea9-4663-8988-a6a9b77bff36?response-cache-control=public%2C%20max-age%3D86400%2C%20immutable&Policy=eyJTdGF0ZW1lbnQiOlt7IlJlc291cmNlIjoiaHR0cHM6Ly9maWxlcy5pbnN0YW50ZGIuY29tL2ZkMTNlNzhhLTFhODktNDRlZC1iMWYzLTM1MDAyMThlZDNmMC8wLzg2YTI5MGMxLTllYTktNDY2My04OTg4LWE2YTliNzdiZmYzNj9yZXNwb25zZS1jYWNoZS1jb250cm9sPXB1YmxpYyUyQyUyMG1heC1hZ2UlM0Q4NjQwMCUyQyUyMGltbXV0YWJsZSIsIkNvbmRpdGlvbiI6eyJEYXRlTGVzc1RoYW4iOnsiQVdTOkVwb2NoVGltZSI6MTc4MzgxNDQwMH19fV19&Signature=PzwSxphBrrWNbDIW7BZvFmkDl6d9tI3aDR6lcSO6C99NUgO8yMi595q5OlwMEUoWyvvHOI51K2gq3Ra5Qc4I9h8ebelKvpLp1mmPozjECVQgqvEB26OTZMHkTWdURXMoIftg4yJoFxfuyAzWETb6mtgXbKHCS428B86MVB9Qbvvv1GR4pGKjW7vfCjH8LTbW2sZ7SUt-wEqxlipNkgllXl4DAK6zMqF3nG-4GY1VOKLXTQldi38vAxi9wYGadJctrnPfLLx8HkaZzy5uFqaBftLKj8SUJ5~tLFXo9okQS~F2NE5UfmTr2n~ZUETtu-ubagh0uCRdr6lkiDXVA09BYQ&Key-Pair-Id=K2D8SOWYIPDBRT",
    alt: "Blueprint schematic of a compact AI server node",
    prompt:
      "Blueprint schematic of a compact 1U AI GPU server node, slightly exploded to show board and connectors, dotted leading lines and measurement ticks. Thin cobalt line-art on light drafting paper, isometric.",
  },
  scale: {
    src: "https://files.instantdb.com/fd13e78a-1a89-44ed-b1f3-3500218ed3f0/9/f7b246eb-40e3-461c-8006-c0c203643544?response-cache-control=public%2C%20max-age%3D86400%2C%20immutable&Policy=eyJTdGF0ZW1lbnQiOlt7IlJlc291cmNlIjoiaHR0cHM6Ly9maWxlcy5pbnN0YW50ZGIuY29tL2ZkMTNlNzhhLTFhODktNDRlZC1iMWYzLTM1MDAyMThlZDNmMC85L2Y3YjI0NmViLTQwZTMtNDYxYy04MDA2LWMwYzIwMzY0MzU0ND9yZXNwb25zZS1jYWNoZS1jb250cm9sPXB1YmxpYyUyQyUyMG1heC1hZ2UlM0Q4NjQwMCUyQyUyMGltbXV0YWJsZSIsIkNvbmRpdGlvbiI6eyJEYXRlTGVzc1RoYW4iOnsiQVdTOkVwb2NoVGltZSI6MTc4MzgxNDQwMH19fV19&Signature=X7DPrfKFmzs6tQLij06WYhwzHhLsdvvNFznMox0-WZJLlAWkTRDXI9UzemD0u5TlNHNIQqTLEAstYucVHdUHIOdPiMPmQpWrFMZ~nqMvX1vQuLhsFwXQa9fjZXXo3c2nVfhLWVe21w2A4Vy31tyEWjqUtcWYhsiVfuZ5BMXtH8jO4p2jhGV6Qv6n9~QX4iI2-0Lacln5kniBCUMQOXtTvT~PE7ytUQc16~SsaNWBMNjiOJLMyYjY~yjRNky00NghztGepi7gTLRLjOxLETckDFijHLqugmIdl79G~iWmV1vem5nkBMCYWfqmKxs23B5TR893-pNHFBiytN0lIQwsqQ&Key-Pair-Id=K2D8SOWYIPDBRT",
    alt: "Blueprint schematic of a scalable data-center rack cluster",
    prompt:
      "Blueprint schematic of a scalable data-center rack cluster: three racks with repeated server sleds, dotted leading lines and dimension annotations. Thin cobalt line-art on light drafting paper, isometric.",
  },
  observe: {
    src: "https://files.instantdb.com/fd13e78a-1a89-44ed-b1f3-3500218ed3f0/7/649bf611-e67d-45ca-b6b0-f8456e160acb?response-cache-control=public%2C%20max-age%3D86400%2C%20immutable&Policy=eyJTdGF0ZW1lbnQiOlt7IlJlc291cmNlIjoiaHR0cHM6Ly9maWxlcy5pbnN0YW50ZGIuY29tL2ZkMTNlNzhhLTFhODktNDRlZC1iMWYzLTM1MDAyMThlZDNmMC83LzY0OWJmNjExLWU2N2QtNDVjYS1iNmIwLWY4NDU2ZTE2MGFjYj9yZXNwb25zZS1jYWNoZS1jb250cm9sPXB1YmxpYyUyQyUyMG1heC1hZ2UlM0Q4NjQwMCUyQyUyMGltbXV0YWJsZSIsIkNvbmRpdGlvbiI6eyJEYXRlTGVzc1RoYW4iOnsiQVdTOkVwb2NoVGltZSI6MTc4MzgxNDQwMH19fV19&Signature=cpNAF5UOjg4mLpbJgCoN8~z19Y6wIqcOjoRXijvajuYrDazPhVfrG5sgz~KX5LzCtJdj2ECU8S28VKZ7s9hcbUmJWlGJq1u8VEJ~ghlN2QRryBxV4RsGKD6lcuGf9Q4Ng7SvzVRmfWRCZmFfpgSuuD2HwwLRaRA339cCW7lsO~VHr6yrQB0M-uNzyWZIvTp8knK7RXE-N0pJdiTAqsqRv8UsUa~bUsLJqbaT7Ce0R8nDJSizoCjw0dE82PzLrZrewop~oWR2QClzRtnKJQdsbJ2uqeOOa4y0Ci0wViYg6nPRfq4u4zjcO4otHOJKwuqf9XfaievDwslR5inie9GsUQ&Key-Pair-Id=K2D8SOWYIPDBRT",
    alt: "Blueprint schematic of an observability sensor module",
    prompt:
      "Blueprint schematic of an observability sensor and telemetry module: circuit board with sensor arrays and probe points, dotted leading lines to measurement nodes, waveform ticks. Thin cobalt line-art on light drafting paper, orthographic.",
  },
  secure: {
    src: "https://files.instantdb.com/fd13e78a-1a89-44ed-b1f3-3500218ed3f0/1/494c1e1b-1af0-4bef-9cf0-0c4b69ce0f8c?response-cache-control=public%2C%20max-age%3D86400%2C%20immutable&Policy=eyJTdGF0ZW1lbnQiOlt7IlJlc291cmNlIjoiaHR0cHM6Ly9maWxlcy5pbnN0YW50ZGIuY29tL2ZkMTNlNzhhLTFhODktNDRlZC1iMWYzLTM1MDAyMThlZDNmMC8xLzQ5NGMxZTFiLTFhZjAtNGJlZi05Y2YwLTBjNGI2OWNlMGY4Yz9yZXNwb25zZS1jYWNoZS1jb250cm9sPXB1YmxpYyUyQyUyMG1heC1hZ2UlM0Q4NjQwMCUyQyUyMGltbXV0YWJsZSIsIkNvbmRpdGlvbiI6eyJEYXRlTGVzc1RoYW4iOnsiQVdTOkVwb2NoVGltZSI6MTc4MzgxNDQwMH19fV19&Signature=IJLiSWJqDQ-SwF5nF1TyEkjV~1JRLzc9wmdN~Z4M1S5OzYRTlO~jKPo1v0g8zRlfKQRPnONTxmR4oXl68kr4JBUGFxxB0futFGc4ZHlJEPMWRSMSKX-EfDY3QyXMMn0J9de0~VivD3BMAFIFrWiru0-yRkhTR6kh~2pDoHysJSSh3q3AJ~wHF4J~W6vCpDjj2jWxxJcwHNdQLAZxLnDfAJDppFBjlsJxQl~fHQmOdO-S0VDrDll0m28fSLK-4N2iwkx2ybVEHSy4knEGrD4Q7h4yjQ~CuzfqIrgd0elTI8biJA8Y7Zk4tR~V5DUkTXhXuaqv-TA7ylHxaK4enQo8ow&Key-Pair-Id=K2D8SOWYIPDBRT",
    alt: "Blueprint schematic of a hardware security enclave module",
    prompt:
      "Blueprint schematic of a hardware security enclave module: secure AI chip package with shielded lid and lock detail, exploded, dotted leading lines to key pins, measurement ticks. Thin cobalt line-art on light drafting paper, isometric.",
  },
  architecture: {
    src: "https://files.instantdb.com/fd13e78a-1a89-44ed-b1f3-3500218ed3f0/0/7b78cb8f-bd0e-4532-921b-6677e5082484?response-cache-control=public%2C%20max-age%3D86400%2C%20immutable&Policy=eyJTdGF0ZW1lbnQiOlt7IlJlc291cmNlIjoiaHR0cHM6Ly9maWxlcy5pbnN0YW50ZGIuY29tL2ZkMTNlNzhhLTFhODktNDRlZC1iMWYzLTM1MDAyMThlZDNmMC8wLzdiNzhjYjhmLWJkMGUtNDUzMi05MjFiLTY2NzdlNTA4MjQ4ND9yZXNwb25zZS1jYWNoZS1jb250cm9sPXB1YmxpYyUyQyUyMG1heC1hZ2UlM0Q4NjQwMCUyQyUyMGltbXV0YWJsZSIsIkNvbmRpdGlvbiI6eyJEYXRlTGVzc1RoYW4iOnsiQVdTOkVwb2NoVGltZSI6MTc4MzgxNDQwMH19fV19&Signature=wXh5xJxGm6RjkrXIW4LRhnjugFCy0MtRkHBbIhj-sB-ecnyfJ86Qus7pXERWd9vebJxl6DljsyD55I9pmfE-V7R9ydEDtzRzaBKuG3zQFFtNxMmRHpNv-4EVPLiTsdyVY6ZiLBAO99Ay~ZGlyGZZ61s14jYP422qL2d5waGsPDipxpBaOw2--ODsW1iHCPzN5sIXtm1NQ7TQeaVPTbhOato3rTo~YqygDHyzwbCDvHdq4ZBIJBgm-jsgqNG73oG5eMdEjpSYYN1svTC7zP10rSmYztrDcwaF0cB9dWXKL7CriAfB7j5~YQfZN9oE6P7sj3idzXLhZAofmlPjiw68EA&Key-Pair-Id=K2D8SOWYIPDBRT",
    alt: "Wide blueprint schematic of the full distributed AI system",
    prompt:
      "Wide blueprint schematic of a full distributed AI infrastructure system: server racks, chips, storage, cooling connected by lines and dotted leading lines showing data flow, numbered node markers, dimension annotations. Thin cobalt line-art on light drafting paper, isometric network diagram.",
  },
  codeBoard: {
    src: "https://files.instantdb.com/fd13e78a-1a89-44ed-b1f3-3500218ed3f0/5/723bde4d-013a-4edb-8232-2b7e7e3a158a?response-cache-control=public%2C%20max-age%3D86400%2C%20immutable&Policy=eyJTdGF0ZW1lbnQiOlt7IlJlc291cmNlIjoiaHR0cHM6Ly9maWxlcy5pbnN0YW50ZGIuY29tL2ZkMTNlNzhhLTFhODktNDRlZC1iMWYzLTM1MDAyMThlZDNmMC81LzcyM2JkZTRkLTAxM2EtNGVkYi04MjMyLTJiN2U3ZTNhMTU4YT9yZXNwb25zZS1jYWNoZS1jb250cm9sPXB1YmxpYyUyQyUyMG1heC1hZ2UlM0Q4NjQwMCUyQyUyMGltbXV0YWJsZSIsIkNvbmRpdGlvbiI6eyJEYXRlTGVzc1RoYW4iOnsiQVdTOkVwb2NoVGltZSI6MTc4MzgxNDQwMH19fV19&Signature=i1tJCIXVHsDumKFRAUdPHAfyJKVxWm0sy0pBPr6ok~YciivPcyfixWSAYkSfRblsT2F2r42qZVT2GN-QxyduOOJEpWmFNtgAIh58Xkuzj~R1WDoV5TyMv0IKm7G1mAmXTMuy53M3TfhSDuQlO3~Jf7BHbkA-T4T3y26jTZyXrbCK6fTwLfFAMrYOn2t3ydwEiiQpUJR5k-NVVAkYNJq7piEOg-HUFiF5OK2urWUo8rxfIbAyva9ZWiuhkXgGvP5AL97nfReENkO~VU92YinibgoPnvG4D6ppFIc8guJkDW27z~yFx36rE7oJw2k5U7rTTZl0HVcoFATKdDdcvyRAcQ&Key-Pair-Id=K2D8SOWYIPDBRT",
    alt: "Blueprint schematic of a dense AI compute circuit board",
    prompt:
      "Blueprint schematic of a dense AI compute circuit board: motherboard with CPU/GPU sockets, memory slots and traces, dotted leading lines to key components, measurement ticks. Thin cobalt line-art on light drafting paper, top-down orthographic.",
  },
  cooling: {
    src: "https://files.instantdb.com/fd13e78a-1a89-44ed-b1f3-3500218ed3f0/7/f0e4e040-0b3f-4d4f-8bd7-f817c86eeea6?response-cache-control=public%2C%20max-age%3D86400%2C%20immutable&Policy=eyJTdGF0ZW1lbnQiOlt7IlJlc291cmNlIjoiaHR0cHM6Ly9maWxlcy5pbnN0YW50ZGIuY29tL2ZkMTNlNzhhLTFhODktNDRlZC1iMWYzLTM1MDAyMThlZDNmMC83L2YwZTRlMDQwLTBiM2YtNGQ0Zi04YmQ3LWY4MTdjODZlZWVhNj9yZXNwb25zZS1jYWNoZS1jb250cm9sPXB1YmxpYyUyQyUyMG1heC1hZ2UlM0Q4NjQwMCUyQyUyMGltbXV0YWJsZSIsIkNvbmRpdGlvbiI6eyJEYXRlTGVzc1RoYW4iOnsiQVdTOkVwb2NoVGltZSI6MTc4MzgxNDQwMH19fV19&Signature=LPGQZBI0kJa~cynb9Sb0GmKTglR5m5nnn41lTcc3AoNCioqNrUFQ86THGm9wYJpGQ-kZWL52EVNfHK5zBDEjVGB8ovYXghFJznZzFTS-Ub7139PCvoqoT8TY0o6gs4ZwOI2fq21XY3kJ51c-Ly9Tc6ACDoEPrs9nOiI9FzZFeDLfkh79ijQnt3o~RsOLlLmfoxyENLQuxq7SVfpSgvpnFeewTSuxsTPxdTnyZ2Be4ua6toUYTV7AFIDDYJelwRiw0QGMGEHAFfMGaNzHAhiI9xAHVd4RnhfENc1KTPl1Ds55fO1Z7Z2U5pcMoaDlVB-Ic7n64jYQ1LKe3YmeSfO4Mg&Key-Pair-Id=K2D8SOWYIPDBRT",
    alt: "Blueprint schematic of a liquid-cooling module for AI servers",
    prompt:
      "Blueprint schematic of a liquid-cooling module for AI servers: cold plate, tubing manifold and radiator exploded, dotted leading lines to coolant ports, flow arrows, dimension ticks. Thin cobalt line-art on light drafting paper, isometric.",
  },
} satisfies Record<string, Figure>;
