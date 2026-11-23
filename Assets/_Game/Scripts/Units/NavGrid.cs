using System.Collections.Generic;
using UnityEngine;
namespace KingOrDeath {
    // Deterministic 2m grid, bridge-aware A*. Dynamic construction invalidates paths.
    public class NavGrid : MonoBehaviour {
        public static NavGrid I;
        public const float Cell=2;
        public const int Width=64, Height=48;
        public int revision;
        bool[,] blocked=new bool[Width,Height];
        void Awake(){ I=this; Rebuild(); }
        public static Vector2Int CellAt(Vector3 p)=>new Vector2Int(Mathf.FloorToInt((p.x+64)/Cell),Mathf.FloorToInt((p.z+48)/Cell));
        public static Vector3 Center(Vector2Int c)=>new Vector3(c.x*Cell-63,0,c.y*Cell-47);
        public static bool InBounds(Vector2Int c)=>c.x>=0&&c.y>=0&&c.x<Width&&c.y<Height;
        public bool Walkable(Vector2Int c)=>InBounds(c)&&!blocked[c.x,c.y];
        public bool Walkable(Vector3 p)=>Walkable(CellAt(p));
        public static bool River(Vector3 p)=>p.x>5&&p.x<13&&Mathf.Abs(p.z+5)>4;
        public void Rebuild(){
            for(int x=0;x<Width;x++)for(int z=0;z<Height;z++)blocked[x,z]=River(Center(new Vector2Int(x,z)));
            foreach(var b in FindObjectsByType<Building>())Block(b.transform.position,b.footprint);
            revision++;
        }
        public void Block(Vector3 p,Vector2 size){
            var a=CellAt(p-new Vector3(size.x/2,0,size.y/2));var b=CellAt(p+new Vector3(size.x/2,0,size.y/2));
            for(int x=a.x;x<=b.x;x++)for(int z=a.y;z<=b.y;z++)if(InBounds(new Vector2Int(x,z)))blocked[x,z]=true;
        }
        public List<Vector3> Path(Vector3 from,Vector3 to){
            var start=CellAt(from);var end=CellAt(to);
            if(!Walkable(start))start=Nearest(start);if(!Walkable(end))end=Nearest(end);
            if(!Walkable(start)||!Walkable(end))return null;
            var open=new List<Vector2Int>{start};var closed=new HashSet<Vector2Int>();
            var previous=new Dictionary<Vector2Int,Vector2Int>();var cost=new Dictionary<Vector2Int,float>{{start,0}};
            while(open.Count>0){
                int best=0;float score=float.MaxValue;
                for(int i=0;i<open.Count;i++){float s=cost[open[i]]+Vector2Int.Distance(open[i],end);if(s<score){score=s;best=i;}}
                var current=open[best];open.RemoveAt(best);
                if(current==end){var result=new List<Vector3>{Center(end)};while(current!=start){current=previous[current];result.Add(Center(current));}result.Reverse();return result;}
                closed.Add(current);
                for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++){
                    if(dx==0&&dz==0)continue;var next=current+new Vector2Int(dx,dz);
                    if(!Walkable(next)||closed.Contains(next))continue;
                    if(dx!=0&&dz!=0&&(!Walkable(current+new Vector2Int(dx,0))||!Walkable(current+new Vector2Int(0,dz))))continue;
                    float g=cost[current]+(dx==0||dz==0?1:1.414214f);
                    if(!cost.ContainsKey(next)||g<cost[next]){cost[next]=g;previous[next]=current;if(!open.Contains(next))open.Add(next);}
                }
            }return null;
        }
        Vector2Int Nearest(Vector2Int c){for(int r=0;r<5;r++)for(int x=-r;x<=r;x++)for(int z=-r;z<=r;z++){var n=c+new Vector2Int(x,z);if(Walkable(n))return n;}return c;}
    }
}
