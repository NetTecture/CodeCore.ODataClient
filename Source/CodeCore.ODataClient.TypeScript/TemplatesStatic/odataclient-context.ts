import { HttpClient, HttpHeaders } from '@angular/common/http';

export class ODataSettings {

  public Url: string;
  public http: HttpClient;
  public headers: HttpHeaders;

}

export class ODataContext {

  constructor() {
    this.ODataSettings = {
      Url: "",
      http: null,
      headers: null,
    };
    this.ODataSettings.headers = new HttpHeaders()
      .set("Content-Type", "application/json")
      .set("Accept", "application/json");
  }

  public readonly ODataSettings: ODataSettings = new ODataSettings();

}


