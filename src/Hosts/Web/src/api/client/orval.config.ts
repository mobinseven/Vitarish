import { defineConfig } from "orval";

const specsDir = "../specs";

export default defineConfig({
  authentication: {
    input: {
      target: `${specsDir}/Main_authentication.json`,
    },
    output: {
      target: "../generated/authentication/authentication.ts",
      schemas: "../generated/authentication/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  journey: {
    input: {
      target: `${specsDir}/Main_journey.json`,
    },
    output: {
      target: "../generated/journey/journey.ts",
      schemas: "../generated/journey/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  users: {
    input: {
      target: `${specsDir}/Main_users.json`,
    },
    output: {
      target: "../generated/users/users.ts",
      schemas: "../generated/users/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  sea: {
    input: {
      target: `${specsDir}/Main_sea.json`,
    },
    output: {
      target: "../generated/sea/sea.ts",
      schemas: "../generated/sea/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  // medical: disabled — Main_medical.json has a broken path param (codeSystemId)
  // in MedicalGetCodeSystem. Re-enable once the backend spec is fixed.
  // medical: {
  //   input: { target: `${specsDir}/Main_medical.json` },
  //   output: {
  //     target: "../generated/medical/medical.ts",
  //     schemas: "../generated/medical/model",
  //     client: "react-query",
  //     override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } },
  //   },
  // },
  parties: {
    input: {
      target: `${specsDir}/Main_parties.json`,
    },
    output: {
      target: "../generated/parties/parties.ts",
      schemas: "../generated/parties/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  files: {
    input: {
      target: `${specsDir}/Main_files.json`,
    },
    output: {
      target: "../generated/files/files.ts",
      schemas: "../generated/files/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  communications: {
    input: {
      target: `${specsDir}/Main_communications.json`,
    },
    output: {
      target: "../generated/communications/communications.ts",
      schemas: "../generated/communications/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  appointments: {
    input: {
      target: `${specsDir}/Main_appointments.json`,
    },
    output: {
      target: "../generated/appointments/appointments.ts",
      schemas: "../generated/appointments/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  search: {
    input: {
      target: `${specsDir}/Main_search.json`,
    },
    output: {
      target: "../generated/search/search.ts",
      schemas: "../generated/search/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  intelligence: {
    input: {
      target: `${specsDir}/Main_intelligence.json`,
    },
    output: {
      target: "../generated/intelligence/intelligence.ts",
      schemas: "../generated/intelligence/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  social: {
    input: {
      target: `${specsDir}/Main_social.json`,
    },
    output: {
      target: "../generated/social/social.ts",
      schemas: "../generated/social/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  feed: {
    input: {
      target: `${specsDir}/Main_feed.json`,
    },
    output: {
      target: "../generated/feed/feed.ts",
      schemas: "../generated/feed/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  knowledge: {
    input: {
      target: `${specsDir}/Main_knowledge.json`,
    },
    output: {
      target: "../generated/knowledge/knowledge.ts",
      schemas: "../generated/knowledge/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  requests: {
    input: {
      target: `${specsDir}/Main_requests.json`,
    },
    output: {
      target: "../generated/requests/requests.ts",
      schemas: "../generated/requests/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  commerce: {
    input: {
      target: `${specsDir}/Main_commerce.json`,
    },
    output: {
      target: "../generated/commerce/commerce.ts",
      schemas: "../generated/commerce/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  clinic: {
    input: {
      target: `${specsDir}/Main_clinic.json`,
    },
    output: {
      target: "../generated/clinic/clinic.ts",
      schemas: "../generated/clinic/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
});
